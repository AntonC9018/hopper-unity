using UnityEngine;
using Core;
using Core.Generation;
using Utils.Vector;
using Core.Behaviors;
using Core.Targeting;
using System.Collections.Generic;
using Core.Items;
using System.Linq;

namespace Hopper
{
    public class Demo : MonoBehaviour
    {
        public GameObject playerPrefab;
        public GameObject enemyPrefab;
        public GameObject wallPrefab;
        public GameObject tilePrefab;

        private World m_world;
        private GameObject m_playerObject;
        private float referenceWidth;
        private List<GameObject> m_enemyObjects;

        void Start()
        {
            Utils.UnitySystemConsoleRedirector.Redirect();

            var moveAction = new BehaviorAction<Moving>();
            var digAction = new BehaviorAction<Digging>();
            var attackAction = new BehaviorAction<Attacking>();

            var defaultAction = new CompositeAction(
                attackAction,
                digAction,
                moveAction
            );

            var playerFactory = new EntityFactory<Player>()
                .AddBehavior<Acting>(new Acting.Config(Algos.SimpleAlgo, null))
                .AddBehavior<Moving>()
                .AddBehavior<Displaceable>()
                .AddBehavior<Controllable>(new Controllable.Config
                {
                    defaultAction = defaultAction
                })
                .AddBehavior<Attackable>()
                .AddBehavior<Damageable>()
                .AddBehavior<Attacking>()
                .Retouch(Core.Retouchers.Skip.EmptyAttack)
                .AddBehavior<Digging>()
                .Retouch(Core.Retouchers.Skip.EmptyDig);

            var rightPiece = new Piece { dir = IntVector2.Right, pos = IntVector2.Right };

            var shovelTargetProvider = new TargetProvider<DigTarget>(
                new List<Piece>(1) { rightPiece },
                Handlers.DigChain);

            var knifeTargetProvider = new TargetProvider<AtkTarget>(
                new List<Piece>(1) { rightPiece },
                Handlers.GeneralChain);

            var shovelItem = new ModularTargetingItem(
                Inventory.ShovelSlot,
                shovelTargetProvider);

            var knifeItem = new ModularTargetingItem(
                Inventory.WeaponSlot,
                knifeTargetProvider);

            var wallFactory = new EntityFactory<Wall>()
                .AddBehavior<Attackable>()
                .Retouch(
                    Core.Retouchers.Attackableness.Constant(AtkCondition.NEVER)
                )
                .AddBehavior<Damageable>();

            var enemyFactory = Test.Skeleton.Factory;

            m_world = new World(50, 50);

            Generator generator = new Generator(50, 50, new Generator.Options
            {
                min_hallway_length = 2,
                max_hallway_length = 5
            });

            generator.AddRoom(new IntVector2(5, 5));
            generator.AddRoom(new IntVector2(5, 5));
            generator.AddRoom(new IntVector2(5, 5));
            generator.AddRoom(new IntVector2(5, 5));
            generator.AddRoom(new IntVector2(5, 5));
            generator.Generate();

            referenceWidth = playerPrefab.GetComponent<SpriteRenderer>().size.x;

            m_enemyObjects = new List<GameObject>();

            for (int y = 0; y < generator.grid.GetLength(1); y++)
            {
                for (int x = 0; x < generator.grid.GetLength(0); x++)
                {
                    if (generator.grid[x, y] != Generator.Mark.EMPTY)
                    {
                        var position = new Vector3(x * referenceWidth, -y * referenceWidth, 0);
                        var instance = Instantiate(tilePrefab, position, Quaternion.identity);

                        if (generator.grid[x, y] == Generator.Mark.WALL)
                        {
                            position = new Vector3(x * referenceWidth, -y * referenceWidth, -1);
                            instance = Instantiate(wallPrefab, position, Quaternion.identity);
                            m_world.SpawnEntity(wallFactory, new IntVector2(x, y));
                        }
                        // just for demo
                        // else if (Random.value > 0.9)
                        // {
                        //     position = new Vector3(x * referenceWidth, -y * referenceWidth, -1);
                        //     instance = Instantiate(enemyPrefab, position, Quaternion.identity);
                        //     m_world.SpawnEntity(enemyFactory, new IntVector2(x, y));
                        //     m_enemyObjects.Add(instance);
                        // }
                    }
                }
            }

            var center = generator.rooms[0].Center.Round();

            var player = m_world.SpawnPlayer(playerFactory, center);

            // player.Inventory.Equip(shovelItem);
            // player.Inventory.Equip(knifeItem);

            m_playerObject = Instantiate(
                playerPrefab,
                new Vector3(center.x * referenceWidth, -center.y * referenceWidth, -1),
                Quaternion.identity);

            Camera.main.transform.position = new Vector3(
                m_playerObject.transform.position.x,
                m_playerObject.transform.position.y,
                Camera.main.transform.position.z);

            {
                var p = new Vector3((center.x + 1) * referenceWidth, -(center.y + 1) * referenceWidth, -1);
                var i = Instantiate(enemyPrefab, p, Quaternion.identity);
                m_world.SpawnEntity(enemyFactory, new IntVector2((center.x + 1), (center.y + 1)));
                m_enemyObjects.Add(i);
            }
        }

        private UnityEngine.KeyCode? LastInput = null;

        // Update is called once per frame
        void Update()
        {
            var map = new Dictionary<KeyCode, ChainName>();
            map.Add(KeyCode.UpArrow, InputMappings.Up);
            map.Add(KeyCode.DownArrow, InputMappings.Down);
            map.Add(KeyCode.LeftArrow, InputMappings.Left);
            map.Add(KeyCode.RightArrow, InputMappings.Right);
            map.Add(KeyCode.Space, InputMappings.Weapon_0);

            // TODO: obviously redo
            if (LastInput != null)
            {
                if (UnityEngine.Input.GetKeyUp(LastInput ?? 0))
                {
                    LastInput = null;
                }
                return;
            }

            if (map.Keys.Any(i => UnityEngine.Input.GetKeyDown(i)))
            {
                // TODO: use virtual buttons
                // Actual button -> virtual button -> input mapping -> action
                // Physical layer -> Unity layer   -> logic layer   
                foreach (var player in m_world.m_state.Players)
                {
                    Action action = null;
                    Controllable input = player.Behaviors.Get<Controllable>();

                    foreach (var code in map.Keys)
                    {
                        if (UnityEngine.Input.GetKeyDown(code))
                        {
                            LastInput = code;
                            action = input.ConvertInputToAction(map[code]);
                            break;
                        }
                    }

                    player.Behaviors.Get<Acting>().NextAction = action;

                    m_world.Loop();

                    {
                        var displacementUpdate = player.History.FindLast(
                           update => update.updateCode == History.UpdateCode.displaced_do);

                        if (displacementUpdate != null)
                        {
                            m_playerObject.transform.position = new Vector3(
                                displacementUpdate.stateAfter.pos.x * referenceWidth,
                                -displacementUpdate.stateAfter.pos.y * referenceWidth,
                                m_playerObject.transform.position.z);

                            Camera.main.transform.position = new Vector3(
                                m_playerObject.transform.position.x,
                                m_playerObject.transform.position.y,
                                Camera.main.transform.position.z);
                        }
                    }

                    var enemies = m_world.m_state.Entities[Layer.REAL.ToIndex()];

                    for (int i = 0; i < enemies.Count; i++)
                    {
                        var displacementUpdate = enemies[i].History.FindLast(
                            update => update.updateCode == History.UpdateCode.displaced_do);

                        if (displacementUpdate != null)
                        {
                            m_enemyObjects[i].transform.position = new Vector3(
                                displacementUpdate.stateAfter.pos.x * referenceWidth,
                                -displacementUpdate.stateAfter.pos.y * referenceWidth,
                                m_enemyObjects[i].transform.position.z);
                        }
                    }

                    if (LastInput != null)
                    {
                        if (UnityEngine.Input.GetKeyUp(LastInput ?? 0))
                        {
                            LastInput = null;
                        }
                    }
                }
            }
        }
    }
}

