﻿using System.Collections.Generic;
using Core;
using Core.Generation;
using Core.History;
using Core.Items;
using Core.Targeting;
using Core.Utils.Vector;

using Hopper.View;
using Hopper.ViewModel;

using Test;

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
        private ModularShovel m_shovelItem;
        private ModularWeapon m_knifeItem;
        private ModularWeapon m_spearItem;
        private View_Model m_viewModel;
        private InputManager m_inputManager;
        private Factories m_factories;

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
            Core.Utils.UnitySystemConsoleRedirector.Redirect();

            CreateItems();
            CreateWorldEvents();

            m_factories = new Factories();

            // Generates the map
            var generator = CreateRunGenerator();

            m_world = new World(generator.grid.GetLength(1), generator.grid.GetLength(0));
            m_world.m_pools.UsePools(itemPool: CreateItemPool(), entityPool: new ThrowawayPool());
            m_world.InitializeWorldEvents();

            // Create view_model and hook it up to watch the world events
            SetupViewModel(m_factories, m_world);

            for (int y = 0; y < generator.grid.GetLength(1); y++)
            {
                for (int x = 0; x < generator.grid.GetLength(0); x++)
                {
                    if (generator.grid[x, y] != Generator.Mark.EMPTY)
                    {
                        TileStuff.CreatedEventPath.Fire(m_world, new IntVector2(x, y));

                        if (generator.grid[x, y] == Generator.Mark.WALL)
                        {
                            m_world.SpawnEntity(m_factories.wallFactory, new IntVector2(x, y));
                        }
                    }
                }
            }

            var center = generator.rooms[0].Center.Round();

            var player = m_world.SpawnPlayer(m_factories.playerFactory, center);

            /* Bounce trap and a wall. */
            // m_world.SpawnEntity(m_factories.trapFactory, center + IntVector2.Right, IntVector2.Right);
            // m_world.SpawnEntity(m_factories.wallFactory, center + IntVector2.Right * 2);

            /* Two bounce traps in a row. */
            // m_world.SpawnEntity(m_factories.trapFactory, center + IntVector2.Right, IntVector2.Right);
            // m_world.SpawnEntity(trapFactory, center + IntVector2.Right * 2, IntVector2.Left);

            /* Uncomment to disable bouncing for player. */
            // player.Stats.GetRaw(Push.Source.Resistance.Path)[BounceTrap.BounceSource.Id] = 3;

            /* A blocking trap. When you step on it, it closes you in. */
            // m_world.SpawnEntity(m_factories.barrierFactory, player.Pos + IntVector2.Right);

            /* A dummy you can attack but it wouldn't take damage */
            // m_world.SpawnEntity(Test.Dummy.Factory, player.Pos + IntVector2.Right);

            /* Knife and Shivel basic equipment. */
            // player.Inventory.Equip(m_knifeItem);
            // player.Inventory.Equip(m_shovelItem);
            // player.Inventory.Equip(m_spearItem);

            /* Bow. X toggle charge, vector input to shoot */
            // player.Inventory.Equip(Bow.DefaultItem);

            /* 10000 bombs. `Space` to use. */
            // player.Inventory.Equip(new PackedItem(Bombing.item, 10000));

            /* Knippers (explody boys). */
            // m_world.SpawnEntity(Knipper.Factory, new IntVector2(center.x + 4, center.y));
            // m_world.SpawnEntity(Knipper.Factory, new IntVector2(center.x + 3, center.y));

            /* A test robot boss that spawns whelps behind itself and shoots lasers. */
            // m_world.SpawnEntity(TestBoss.Factory, new IntVector2(center.x + 4, center.y));

            /* A barrier block blocks movement only through one side. The second coordinate is the 
               orientation, which for such blocks defines what side of the cell it is at.
            */
            // m_world.SpawnEntity(m_factories.barrierFactory,
            //    new IntVector2(center.x + 2, center.y), new IntVector2(-1, 0));

            /* A chest contains something depending on the factory. It may spawn preset entities,
               items or draw them from specified in the config pools and gold.
               See how the factory is defined.
            */
            // m_world.SpawnEntity(m_factories.chestFactory, new IntVector2(center.x + 1, center.y + 1));

            /* Water blocks stop one movement, attack or dig. */
            // m_world.SpawnEntity(m_factories.waterFactory, new IntVector2(center.x + 1, center.y + 1));
            // m_world.SpawnEntity(m_factories.waterFactory, new IntVector2(center.x, center.y + 1));

            /* Ice makes you slide, skipping movement, attack or dig. */
            // m_world.SpawnEntity(m_factories.iceFactory, new IntVector2(center.x - 1, center.y - 1));
            // m_world.SpawnEntity(m_factories.iceFactory, new IntVector2(center.x, center.y - 1));
            // m_world.SpawnEntity(m_factories.iceFactory, new IntVector2(center.x + 1, center.y - 1));

            /* Apply freezing on player. */
            // FreezeStatus.Status.TryApply(player, new FreezeData(), FreezeStat.Path.DefaultFile);
        }

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

            return pool;
            // return pool.Copy();
        }

        private void SetupViewModel(Factories factories, World world)
        {
            var destroyOnDeathSieve = new SimpleSieve(AnimationCode.Destroy, UpdateCode.dead);
            var playerJumpSieve = new SimpleSieve(AnimationCode.Jump, UpdateCode.move_do);

            View.Timer timer = gameObject.AddComponent<View.Timer>();
            var animator = new ViewAnimator(new SceneEnt(Camera.main.gameObject), timer);

            m_viewModel = new View_Model(animator);
            m_viewModel.SetDefaultPrefab(new Prefab<SceneEnt>(defaultPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(factories.playerFactory.Id,
                new Prefab<SceneEnt>(playerPrefab, destroyOnDeathSieve, playerJumpSieve));
            m_viewModel.SetPrefabForFactory(factories.enemyFactory.Id,
                new Prefab<SceneEnt>(enemyPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(factories.wallFactory.Id,
                new Prefab<SceneEnt>(wallPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(factories.chestFactory.Id,
                new Prefab<SceneEnt>(chestPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(BombEntity.Factory.Id,
                new Prefab<SceneEnt>(bombPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(DroppedItem.Factory.Id,
                new Prefab<RegularRotationSceneEnt>(droppedItemPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(factories.waterFactory.Id,
                new Prefab<SceneEnt>(waterPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(factories.iceFactory.Id,
                new Prefab<SceneEnt>(icePrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(factories.trapFactory.Id,
                new Prefab<SceneEnt>(bounceTrapPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(factories.barrierFactory.Id,
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

        private void CreateItems()
        {
            var knifeTargetProvider = TargetProvider.CreateAtk(
                Pattern.Default,
                Handlers.GeneralChain
            );

            m_shovelItem = new ModularShovel(
                new ItemMetadata("Base_Shovel"),
                TargetProvider.SimpleDig
            );

            m_knifeItem = new ModularWeapon(
                new ItemMetadata("Base_Knife"),
                knifeTargetProvider
            );

            m_spearItem = new ModularWeapon(
                new ItemMetadata("Base_Spear"),
                TargetProvider.CreateAtk(
                    new Pattern(
                        new Piece
                        {
                            dir = IntVector2.Right,
                            pos = IntVector2.Right,
                            reach = null,
                        },
                        new Piece
                        {
                            dir = IntVector2.Right,
                            pos = IntVector2.Right * 2,
                            reach = new List<int>(1) { 1 }
                        }
                    ),
                    Handlers.GeneralChain
                )
            );

            // Reference this item once so that it is added to the registry
            var _ = Bow.DefaultItem;
        }

        public void CreateWorldEvents()
        {
            var _ = Explosion.EventPath;
            var __ = Laser.EventPath;
            var ___ = TileStuff.CreatedEventPath;

            var worldEventRegistry = Registry.Default.GetKindRegistry<IWorldEvent>();
            worldEventRegistry.SetServerMap(worldEventRegistry.PackModMap());
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

