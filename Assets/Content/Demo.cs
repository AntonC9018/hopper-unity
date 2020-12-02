using UnityEngine;
using Core;
using Core.Generation;
using Core.Utils.Vector;
using Core.Behaviors;
using Core.Targeting;
using System.Collections.Generic;
using Core.Items;
using System.Linq;
using Core.Utils;
using Test;
using Core.History;
using Hopper.ViewModel;
using Hopper.View;
using Core.Stats.Basic;

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
        private View_Model m_viewModel;


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
            Core.Utils.UnitySystemConsoleRedirector.Redirect();

            CreateItems();

            var itemPool = CreateItemPool();
            var entityPool = new ThrowawayPool();

            ContentProvider.DefaultProvider.UsePools(entityPool, itemPool);

            var playerFactory = CreatePlayerFactory().AddBehavior<Statused>();

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

            var waterFactory = Water.CreateFactory();
            var iceFactory = IceFloor.CreateFactory();
            var trapFactory = BounceTrap.Factory;
            var barrierFactory = BlockingTrap.BarrierFactory;

            var itemMap = Registry.Default.Items.PackModMap();
            Registry.Default.Items.SetServerMap(itemMap);

            var generator = CreateGenerator();
            generator.Generate();
            m_world = new World(generator.grid.GetLength(1), generator.grid.GetLength(0));

            var destroyOnDeathSieve = new SimpleSieve(AnimationCode.Destroy, UpdateCode.dead);
            var playerJumpSieve = new SimpleSieve(AnimationCode.Jump, UpdateCode.move_do);

            View.Timer timer = gameObject.AddComponent<View.Timer>();
            var animator = new ViewAnimator(new SceneEnt(Camera.main.gameObject), timer);

            m_viewModel = new View_Model(animator);
            m_viewModel.SetDefaultPrefab(new Prefab<SceneEnt>(defaultPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(playerFactory.Id,
                new Prefab<SceneEnt>(playerPrefab, destroyOnDeathSieve, playerJumpSieve));
            m_viewModel.SetPrefabForFactory(enemyFactory.Id,
                new Prefab<SceneEnt>(enemyPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(wallFactory.Id,
                new Prefab<SceneEnt>(wallPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(chestFactory.Id,
                new Prefab<SceneEnt>(chestPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(BombEntity.Factory.Id,
                new Prefab<SceneEnt>(bombPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(DroppedItem.Factory.Id,
                new Prefab<RegularRotationSceneEnt>(droppedItemPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(waterFactory.Id,
                new Prefab<SceneEnt>(waterPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(iceFactory.Id,
                new Prefab<SceneEnt>(icePrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(trapFactory.Id,
                new Prefab<SceneEnt>(bounceTrapPrefab, destroyOnDeathSieve));
            m_viewModel.SetPrefabForFactory(barrierFactory.Id,
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
            m_viewModel.WatchWorld(m_world, explosionWatcher, tileWatcher, laserBeamWatcher);

            Reference.Width = playerPrefab.GetComponent<SpriteRenderer>().size.x;

            // m_candaceAnimationManager.SetWorld(m_world);

            for (int y = 0; y < generator.grid.GetLength(1); y++)
            {
                for (int x = 0; x < generator.grid.GetLength(0); x++)
                {
                    if (generator.grid[x, y] != Generator.Mark.EMPTY)
                    {
                        TileStuff.FireCreatedEvent(new IntVector2(x, y));

                        if (generator.grid[x, y] == Generator.Mark.WALL)
                        {
                            m_world.SpawnEntity(wallFactory, new IntVector2(x, y));
                        }
                    }
                }
            }

            var center = generator.rooms[0].Center.Round();

            var _ = Bow.DefaultItem; // this is required to reference the slot at least once
            // player.Inventory.Equip(m_knifeItem);

            var player = m_world.SpawnPlayer(playerFactory, center);
            m_world.SpawnEntity(trapFactory, center + IntVector2.Right, IntVector2.Right);
            // player.Stats.GetRaw(Push.Source.Resistance.Path)[BounceTrap.BounceSource.Id] = 20;
            m_world.SpawnEntity(wallFactory, center + IntVector2.Right * 2);

            // m_world.SpawnEntity(trapFactory, center + IntVector2.Right * 2, IntVector2.Left);

            // m_world.SpawnEntity(BlockingTrap.CreateFactory(), player.Pos + IntVector2.Right);

            // player.Inventory.Equip(Bow.DefaultItem);
            // player.Inventory.Equip(m_knifeItem);
            // player.Inventory.Equip(new PackedItem(Bombing.item, 10000));
            // player.Inventory.Equip(shovelItem);
            // player.Inventory.Equip(knifeItem);

            // m_world.SpawnEntity(Knipper.Factory, new IntVector2(center.x + 4, center.y));
            // m_world.SpawnEntity(Knipper.Factory, new IntVector2(center.x + 3, center.y));

            // m_world.SpawnEntity(TestBoss.Factory, new IntVector2(center.x + 4, center.y));
            // m_world.SpawnEntity(barrierFactory, new IntVector2(center.x + 2, center.y), new IntVector2(-1, 0));

            // m_world.SpawnEntity(chestFactory, new IntVector2(center.x + 1, center.y + 1));
            // m_world.SpawnEntity(waterFactory, new IntVector2(center.x + 1, center.y + 1));
            // m_world.SpawnEntity(waterFactory, new IntVector2(center.x, center.y + 1));
            // m_world.SpawnEntity(iceFactory, new IntVector2(center.x - 1, center.y - 1));
            // m_world.SpawnEntity(iceFactory, new IntVector2(center.x, center.y - 1));
            // m_world.SpawnEntity(iceFactory, new IntVector2(center.x + 1, center.y - 1));
            // var trap0 = m_world.SpawnEntity(trapFactory, new IntVector2(center.x - 2, center.y - 1));
            // trap0.Reorient(new IntVector2(1, 0));
            // FreezeStatus.Status.TryApply(player, new FreezeData(), FreezeStat.Path.DefaultFile);
        }

        private UnityEngine.KeyCode? LastInput = null;

        // Update is called once per frame
        void Update()
        {
            var map = new Dictionary<KeyCode, InputMapping>{
                { KeyCode.UpArrow, InputMapping.Up },
                { KeyCode.DownArrow, InputMapping.Down },
                { KeyCode.LeftArrow, InputMapping.Left },
                { KeyCode.RightArrow, InputMapping.Right },
                { KeyCode.Space, InputMapping.Weapon_0 },
                { KeyCode.X, InputMapping.Weapon_1 },
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
                foreach (var player in m_world.State.Players)
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
                    var target = actor.GetCellRelative(action.direction)?.GetAnyEntityFromLayer(Layer.REAL);
                    if (target == null) return false;
                    var interactable = target.Behaviors.TryGet<Interactable>();
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
                shovelTargetProvider
            );

            m_knifeItem = new ModularWeapon(
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

