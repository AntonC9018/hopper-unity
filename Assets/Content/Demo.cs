using UnityEngine;
using Core;
using Core.Generation;
using Utils.Vector;
using Core.Behaviors;
using Core.Targeting;
using System.Collections.Generic;
using Core.Items;
using System.Linq;
using Core.Utils;
using Test;
using Core.History;

namespace Hopper
{
    public class Demo : MonoBehaviour
    {
        public GameObject playerPrefab;
        public GameObject enemyPrefab;
        public GameObject wallPrefab;
        public GameObject tilePrefab;
        public GameObject chestPrefab;
        public GameObject droppedItemPrefab;

        private CandaceAnimationManager m_candaceAnimationManager;

        private World m_world;
        private GameObject m_playerObject;
        private float m_referenceWidth;
        private List<GameObject> m_enemyObjects;
        private List<GameObject> m_droppedItemsObjects;

        private ModularShovel m_shovelItem;
        private ModularWeapon m_knifeItem;


        private ISuperPool CreateItemPool()
        {
            PoolItem[] items = new[]
            {
                new PoolItem(m_knifeItem.Id, 1),
                new PoolItem(m_shovelItem.Id, 1),
                // new PoolItem(Bombing.item.Id, 1),
                new PoolItem(Bombing.item_x3.Id, 20)
            };

            var pool = Pool.CreateNormal<IItem>();

            pool.Add("zone1/weapons", items[0]);
            pool.Add("zone1/shovels", items[1]);
            pool.Add("zone1/stuff", items[2]);
            // pool.Add("zone1/stuff", items[3]);

            return pool.Copy();
        }

        void Start()
        {
            Utils.UnitySystemConsoleRedirector.Redirect();

            m_candaceAnimationManager = GetComponent<CandaceAnimationManager>();

            CreateItems();

            var itemPool = CreateItemPool();
            var entityPool = new ThrowawayPool();

            ContentProvider.DefaultProvider.UsePools(entityPool, itemPool);

            var playerFactory = CreatePlayerFactory();

            var wallFactory = new EntityFactory<Wall>()
                .AddBehavior<Attackable>()
                .Retouch(
                    Core.Retouchers.Attackableness.Constant(AtkCondition.NEVER)
                )
                .AddBehavior<Damageable>();

            var enemyFactory = Test.Skeleton.Factory;

            var chestFactory = new EntityFactory<Entity>()
                .AddBehavior<Interactable>(
                    new Interactable.Config
                    {
                        contentConfig = new ContentConfig
                        {
                            type = ContentType.ITEM,
                            isAforeset = false,
                            poolPath = "zone1/stuff"
                        }
                    }
                );

            var itemMap = IdMap.Items.PackModMap();
            IdMap.Items.SetServerMap(itemMap);

            var generator = CreateGenerator();
            generator.Generate();
            m_world = new World(generator.grid.GetLength(1), generator.grid.GetLength(0));

            m_candaceAnimationManager.SetWorld(m_world); //


            m_referenceWidth = playerPrefab.GetComponent<SpriteRenderer>().size.x;
            m_enemyObjects = new List<GameObject>();
            m_droppedItemsObjects = new List<GameObject>();

            for (int y = 0; y < generator.grid.GetLength(1); y++)
            {
                for (int x = 0; x < generator.grid.GetLength(0); x++)
                {
                    if (generator.grid[x, y] != Generator.Mark.EMPTY)
                    {
                        var position = new Vector3(x * m_referenceWidth, -y * m_referenceWidth, 0);
                        var instance = Instantiate(tilePrefab, position, Quaternion.identity);

                        if (generator.grid[x, y] == Generator.Mark.WALL)
                        {
                            position = new Vector3(x * m_referenceWidth, -y * m_referenceWidth, -1);
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
            player.Inventory.Equip(m_knifeItem);

            // player.Inventory.Equip(shovelItem);
            // player.Inventory.Equip(knifeItem);

            m_playerObject = Instantiate(
                playerPrefab,
                new Vector3(center.x * m_referenceWidth, -center.y * m_referenceWidth, -1),
                Quaternion.identity);

            m_candaceAnimationManager.SetPlayerAnimator(m_playerObject.GetComponent<Animator>());



            Camera.main.transform.position = new Vector3(
                m_playerObject.transform.position.x,
                m_playerObject.transform.position.y,
                Camera.main.transform.position.z);

            {
                var pos = new Vector3(
                    (center.x + 1) * m_referenceWidth,
                    -(center.y + 1) * m_referenceWidth,
                    -1);
                var ins = Instantiate(chestPrefab, pos, Quaternion.identity);
                m_world.SpawnEntity(
                    chestFactory,
                    new IntVector2(center.x + 1, center.y + 1));
                m_enemyObjects.Add(ins);
            }
        }

        private UnityEngine.KeyCode? LastInput = null;

        // Update is called once per frame
        void Update()
        {
            var map = new Dictionary<KeyCode, ChainName>{
                { KeyCode.UpArrow, InputMappings.Up },
                { KeyCode.DownArrow, InputMappings.Down },
                { KeyCode.LeftArrow, InputMappings.Left },
                { KeyCode.RightArrow, InputMappings.Right },
                { KeyCode.Space, InputMappings.Weapon_0 },
            };

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
                    // System.Console.WriteLine(player.Pos);
                    // if (m_world.m_state.m_numIters == 0)
                    //     Explosion.Explode(player.Pos + IntVector2.Left, 1, m_world);
                    // System.Console.WriteLine(player.Pos);
                    m_world.Loop();
                    // System.Console.WriteLine(player.Pos);


                    {
                        var displacementUpdate = player.History.Updates.FindLast(
                           update => update.updateCode == UpdateCode.displaced_do);

                        if (displacementUpdate != null)
                        {
                            m_playerObject.transform.position = new Vector3(
                                displacementUpdate.stateAfter.pos.x * m_referenceWidth,
                                -displacementUpdate.stateAfter.pos.y * m_referenceWidth,
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
                        var displacementUpdate = enemies[i].History.Updates.FindLast(
                            update => update.updateCode == UpdateCode.displaced_do);

                        if (displacementUpdate != null)
                        {
                            m_enemyObjects[i].transform.position = new Vector3(
                                displacementUpdate.stateAfter.pos.x * m_referenceWidth,
                                -displacementUpdate.stateAfter.pos.y * m_referenceWidth,
                                m_enemyObjects[i].transform.position.z);
                        }
                    }

                    // for (int j = enemies.Count; j < m_enemyObjects.Count; j++)
                    // {
                    //     Destroy(m_enemyObjects[j]);
                    // }
                    // if (m_enemyObjects.Count - enemies.Count > 0)
                    // {
                    //     m_enemyObjects.RemoveRange(enemies.Count, m_enemyObjects.Count - enemies.Count);
                    // }

                    var dropped = m_world.m_state.Entities[Layer.DROPPED.ToIndex()];

                    if (m_droppedItemsObjects.Count < dropped.Count)
                    {
                        var count = m_droppedItemsObjects.Count;

                        for (int j = count; j < dropped.Count; j++)
                        {
                            var droppedItem = dropped[j];
                            var pos = new Vector3(droppedItem.Pos.x * m_referenceWidth, -droppedItem.Pos.y * m_referenceWidth, -1);
                            var instance = Instantiate(droppedItemPrefab, pos, Quaternion.identity);
                            m_droppedItemsObjects.Add(instance);
                        }
                    }

                    for (int j = dropped.Count; j < m_droppedItemsObjects.Count; j++)
                    {
                        Destroy(m_droppedItemsObjects[j]);
                    }
                    if (m_droppedItemsObjects.Count - dropped.Count > 0)
                    {
                        m_droppedItemsObjects.RemoveRange(dropped.Count, m_droppedItemsObjects.Count - dropped.Count);
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
        private EntityFactory<Player> CreatePlayerFactory()
        {
            var moveAction = new BehaviorAction<Moving>();
            var digAction = new BehaviorAction<Digging>();
            var attackAction = new BehaviorAction<Attacking>();
            var interactAction = new SimpleAction(
                (Entity actor, Action action) =>
                {
                    var target = actor.GetCellRelative(action.direction)?.GetEntityFromLayer(Layer.REAL);
                    if (target == null) return false;
                    var interactable = target.Behaviors.Get<Interactable>();
                    if (interactable == null) return false;
                    return interactable.Activate();
                }
            );

            var defaultAction = new CompositeAction(
                interactAction,
                attackAction,
                digAction,
                moveAction
            );

            var playerFactory = Player.CreateFactory()
                .AddBehavior<Controllable>(new Controllable.Config
                {
                    defaultAction = defaultAction
                });

            return playerFactory;
        }

        private void CreateItems()
        {
            var shovelTargetProvider = TargetProvider.CreateDig(
                Pattern.Default,
                Handlers.DigChain
            );

            var knifeTargetProvider = TargetProvider.CreateAtk(
                Pattern.Default,
                Handlers.GeneralChain
            );

            m_shovelItem = new ModularShovel(
                Inventory.ShovelSlot,
                shovelTargetProvider
            );

            m_knifeItem = new ModularWeapon(
                Inventory.WeaponSlot,
                knifeTargetProvider
            );
        }

        private Generator CreateGenerator()
        {
            Generator generator = new Generator(11, 11, new Generator.Options
            {
                min_hallway_length = 2,
                max_hallway_length = 5
            });

            generator.AddRoom(new IntVector2(10, 10));
            // generator.AddRoom(new IntVector2(5, 5));
            // generator.AddRoom(new IntVector2(5, 5));
            // generator.AddRoom(new IntVector2(5, 5));
            // generator.AddRoom(new IntVector2(5, 5));
            // generator.Generate();
            return generator;
        }
    }
}

