using Hopper.Core;
using Hopper.Core.Generation;
using Hopper.Core.History;
using Hopper.Core.Items;
using Hopper.Core.Mods;
using Hopper.Test_Content;
using Hopper.Test_Content.Boss;
using Hopper.Test_Content.Explosion;
using Hopper.Test_Content.Floor;
using Hopper.Test_Content.SimpleMobs;
using Hopper.Test_Content.Status.Freeze;
using Hopper.Test_Content.Trap;
using Hopper.Utils.Vector;

using Hopper.View;
using Hopper.ViewModel;
using UnityEngine;

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
        public GameObject bombPrefab;
        public GameObject explosionPrefab;
        public GameObject waterPrefab;
        public GameObject icePrefab;
        public GameObject bounceTrapPrefab;
        public GameObject barrierPrefab;
        public GameObject knipperPrefab;
        public GameObject testBossPrefab;
        public GameObject whelpPrefab;
        public GameObject laserBeamHeadPrefab;
        public GameObject laserBeamBodyPrefab;

        public GameObject defaultPrefab;

        private World m_world;
        private View_Model m_viewModel;
        private InputManager m_inputManager;

        private void Update()
        {
            if (m_inputManager.TrySetAction(m_world))
            {
                m_world.Loop();
            }
        }

        private void Start()
        {
            m_inputManager = new InputManager();

            // Redirects System.Console.WriteLine to unity's console. By default, it goes to debug logs.
            Hopper.Utils.UnitySystemConsoleRedirector.Redirect();

            var modManager = new ModManager();
            modManager.Add<TestMod>();
            modManager.Add<DemoMod>();
            var result = modManager.RegisterAll();
            var demoMod = result.mods.Get<DemoMod>();

            // Generates the map
            var generator = CreateRunGenerator();

            m_world = new World(generator.grid.GetLength(1), generator.grid.GetLength(0), result.patchArea);
            m_world.m_pools.UsePools(itemPool: CreateItemPool(demoMod), entityPool: new ThrowawayPool());
            m_world.InitializeWorldEvents();

            // Create view_model and hook it up to watch the world events
            SetupViewModel(demoMod, m_world);

            for (int y = 0; y < generator.grid.GetLength(1); y++)
            {
                for (int x = 0; x < generator.grid.GetLength(0); x++)
                {
                    if (generator.grid[x, y] != Generator.Mark.EMPTY)
                    {
                        TileStuff.CreatedEventPath.Fire(m_world, new IntVector2(x, y));

                        if (generator.grid[x, y] == Generator.Mark.WALL)
                        {
                            // m_world.SpawnEntity(m_factories.wallFactory, new IntVector2(x, y));
                        }
                    }
                }
            }

            var center = generator.rooms[0].Center.Round();

            var player = m_world.SpawnPlayer(demoMod.PlayerFactory, center);

            /* Bounce trap and a wall. */
            // m_world.SpawnEntity(BounceTrap.Factory, center + IntVector2.Right, IntVector2.Right);
            // m_world.SpawnEntity(demoMod.WallFactory, center + IntVector2.Right * 2);

            /* Two bounce traps in a row. */
            // m_world.SpawnEntity(BounceTrap.Factory, center + IntVector2.Right, IntVector2.Right);
            // m_world.SpawnEntity(BounceTrap.Factory, center + IntVector2.Right * 2, IntVector2.Left);

            /* Uncomment to disable bouncing for player. */
            // player.Stats.GetRaw(Push.Source.Resistance.Path)[Bounce.Source.GetId(registry)] = 3;

            /* A blocking trap. When you step on it, it closes you in. */
            // m_world.SpawnEntity(BlockingTrap.Factory, player.Pos + IntVector2.Right);

            /* A dummy you can attack but it wouldn't take damage */
            // m_world.SpawnEntity(Dummy.Factory, player.Pos + IntVector2.Right);

            /* Knife and Shovel basic equipment. */
            player.Inventory.Equip(demoMod.KnifeItem);
            player.Inventory.Equip(demoMod.ShovelItem);
            player.Inventory.Equip(demoMod.SpearItem);

            /* Bow. X toggle charge, vector input to shoot */
            // player.Inventory.Equip(testMod.Item.DefaultBow);

            /* 10000 bombs. `Space` to use. */
            // player.Inventory.Equip(new PackedItem(new ItemMetadata("bombs"), Bomb.Item, 10000));

            /* Knippers (explody boys). */
            // m_world.SpawnEntity(Knipper.Factory, new IntVector2(center.x + 4, center.y));
            // m_world.SpawnEntity(Knipper.Factory, new IntVector2(center.x + 3, center.y));

            /* A test robot boss that spawns whelps behind itself and shoots lasers. */
            // m_world.SpawnEntity(TestBoss.Factory, new IntVector2(center.x + 4, center.y));

            /* A barrier block blocks movement only through one side. The second coordinate is the 
               orientation, which for such blocks defines what side of the cell it is at.
            */
            // m_world.SpawnEntity(Barrier.Factory,
            //    new IntVector2(center.x + 2, center.y), new IntVector2(-1, 0));

            /* A chest contains something depending on the factory. It may spawn preset entities,
               items or draw them from specified in the config pools and gold.
               See how the factory is defined.
            */
            // m_world.SpawnEntity(demoMod.ChestFactory, new IntVector2(center.x + 1, center.y + 1));

            /* Water blocks stop one movement, attack or dig. */
            // m_world.SpawnEntity(Water.Factory, new IntVector2(center.x + 1, center.y + 1));
            // m_world.SpawnEntity(Water.Factory, new IntVector2(center.x, center.y + 1));

            /* Ice makes you slide, skipping movement, attack or dig. */
            // m_world.SpawnEntity(IceFloor.Factory, new IntVector2(center.x - 1, center.y - 1));
            // m_world.SpawnEntity(IceFloor.Factory, new IntVector2(center.x, center.y - 1));
            // m_world.SpawnEntity(IceFloor.Factory, new IntVector2(center.x + 1, center.y - 1));

            /* Apply freezing on player. */
            FreezeStatus.Status.TryApply(player, new FreezeData(), FreezeStat.Path.defaultFile);
        }

        private ISuperPool CreateItemPool(DemoMod mod)
        {
            PoolItem[] items = new[]
            {
                new PoolItem(mod.KnifeItem.Id, 1),
                new PoolItem(mod.ShovelItem.Id, 1)
                // new PoolItem(Bombing.item.Id, 1),
                // new PoolItem(Bombing.item_x3.Id, 20)
            };

            var pool = Pool.CreateNormal();

            pool.AddItemToSubpool("zone1/weapons", items[0]);
            pool.AddItemToSubpool("zone1/shovels", items[1]);
            // pool.AddItemToSubpool("zone1/stuff", items[2]);
            // pool.Add("zone1/stuff", items[3]);

            return pool;
            // return pool.Copy();
        }

        private void SetupViewModel(DemoMod demoMod, World world)
        {
            var destroyOnDeathSieve = new SimpleSieve(AnimationCode.Destroy, UpdateCode.dead);
            var playerJumpSieve = new SimpleSieve(AnimationCode.Jump, UpdateCode.move_do);

            View.Timer timer = gameObject.AddComponent<View.Timer>();
            var animator = new ViewAnimator(new SceneEnt(Camera.main.gameObject), timer);

            m_viewModel = new View_Model(animator);
            m_viewModel.SetDefaultPrefab(new Prefab<SceneEnt>(defaultPrefab, destroyOnDeathSieve));

            m_viewModel.SetPrefabForFactory(demoMod.PlayerFactory.Id,
                new Prefab<SceneEnt>(playerPrefab, destroyOnDeathSieve, playerJumpSieve));
            m_viewModel.SetPrefabForFactory(demoMod.WallFactory.Id,
                new Prefab<SceneEnt>(wallPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(demoMod.ChestFactory.Id,
                new Prefab<SceneEnt>(chestPrefab, destroyOnDeathSieve));

            m_viewModel.SetPrefabForFactory(Skeleton.Factory.Id,
                new Prefab<SceneEnt>(enemyPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(BombEntity.Factory.Id,
                new Prefab<SceneEnt>(bombPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(DroppedItem.Factory.Id,
                new Prefab<RegularRotationSceneEnt>(droppedItemPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(Water.Factory.Id,
                new Prefab<SceneEnt>(waterPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(IceFloor.Factory.Id,
                new Prefab<SceneEnt>(icePrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(BounceTrap.Factory.Id,
                new Prefab<SceneEnt>(bounceTrapPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(Barrier.Factory.Id,
                new Prefab<RegularRotationSceneEnt>(barrierPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(RealBarrier.Factory.Id,
                new Prefab<RegularRotationSceneEnt>(barrierPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(Knipper.Factory.Id,
                new Prefab<SceneEnt>(knipperPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(TestBoss.Factory.Id,
                new Prefab<SceneEnt>(testBossPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(TestBoss.Whelp.Factory.Id,
                new Prefab<SceneEnt>(whelpPrefab, destroyOnDeathSieve));

            var explosionWatcher = new ExplosionWatcher(explosionPrefab);
            var laserBeamWatcher = new LaserBeamWatcher(laserBeamHeadPrefab, laserBeamBodyPrefab);
            var tileWatcher = new TileWatcher(new Prefab<SceneEnt>(tilePrefab));

            Reference.Width = playerPrefab.GetComponent<SpriteRenderer>().size.x;

            m_viewModel.WatchWorld(world, explosionWatcher, tileWatcher, laserBeamWatcher);
        }

        private Generator CreateRunGenerator()
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
            generator.Generate();
            return generator;
        }
    }
}

