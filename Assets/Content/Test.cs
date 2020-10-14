using UnityEngine;
using Core;
using Core.Generation;
using Utils.Vector;
using Core.Behaviors;

namespace Hopper
{
    public class Test : MonoBehaviour
    {
        public GameObject playerPrefab;
        public GameObject enemyPrefab;
        public GameObject wallPrefab;
        public GameObject tilePrefab;

        private World m_world;
        private GameObject m_playerObject;
        private float referenceWidth;

        void Start()
        {
            Utils.UnitySystemConsoleRedirector.Redirect();

            var moveAction = new BehaviorAction<Moving>();

            var playerFactory = new EntityFactory<Player>()
                .AddBehavior<Acting>(new Acting.Config
                {
                    DoAction = Algos.SimpleAlgo
                })
                .AddBehavior<Moving>()
                .AddBehavior<Displaceable>()
                .AddBehavior<Controllable>(new Controllable.Config
                {
                    defaultAction = moveAction
                });

            m_world = new World(50, 50);

            Generator generator = new Generator(50, 50, new Generator.Options
            {
                min_hallway_length = 2,
                max_hallway_length = 5
            });
            generator.graph.Print();
            generator.AddRoom(new IntVector2(5, 5));
            generator.graph.Print();
            generator.AddRoom(new IntVector2(5, 5));
            generator.graph.Print();
            generator.AddRoom(new IntVector2(5, 5));
            generator.graph.Print();
            generator.AddRoom(new IntVector2(5, 5));
            generator.graph.Print();
            generator.AddRoom(new IntVector2(5, 5));
            generator.graph.Print();
            generator.Generate();

            referenceWidth = playerPrefab.GetComponent<SpriteRenderer>().size.x;

            for (int y = 0; y < generator.grid.GetLength(1); y++)
            {
                for (int x = 0; x < generator.grid.GetLength(0); x++)
                {
                    if (generator.grid[x, y] != Generator.Mark.EMPTY)
                    {
                        var position = new Vector3(x * referenceWidth, -y * referenceWidth, 0);
                        var instance = Instantiate(tilePrefab, position, Quaternion.identity);
                    }
                }
            }

            var center = generator.rooms[0].Center.Round();

            var player = m_world.SpawnPlayer(playerFactory, center);

            m_playerObject = Instantiate(
                playerPrefab,
                new Vector3(center.x * referenceWidth, -center.y * referenceWidth, -1),
                Quaternion.identity);

            Camera.main.transform.position = new Vector3(
                m_playerObject.transform.position.x,
                m_playerObject.transform.position.y,
                Camera.main.transform.position.z);
        }

        private UnityEngine.KeyCode? LastInput = null;

        // Update is called once per frame
        void Update()
        {
            // TODO: obviously redo. this is fucking dumb
            if (LastInput != null)
            {
                if (UnityEngine.Input.GetKeyUp(LastInput ?? 0))
                {
                    LastInput = null;
                }
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)
                || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                // TODO: use virtual buttons
                // Actual button -> virtual button -> input mapping -> action
                // Physical layer -> Unity layer   -> logic layer   
                foreach (var player in m_world.m_state.Players)
                {
                    Action action;
                    Controllable input = player.Behaviors.Get<Controllable>();

                    if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        action = input.ConvertInputToAction(InputMappings.Up);
                        LastInput = KeyCode.UpArrow;
                    }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        action = input.ConvertInputToAction(InputMappings.Down);
                        LastInput = KeyCode.DownArrow;
                    }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        action = input.ConvertInputToAction(InputMappings.Left);
                        LastInput = KeyCode.LeftArrow;
                    }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        action = input.ConvertInputToAction(InputMappings.Right);
                        LastInput = KeyCode.RightArrow;
                    }
                    else
                    {
                        LastInput = null;
                        action = null;
                    }

                    // TODO: won't work if action is null. The bug is in the Acting Behavior, not here
                    player.Behaviors.Get<Acting>().NextAction = action;

                    m_world.Loop();

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

