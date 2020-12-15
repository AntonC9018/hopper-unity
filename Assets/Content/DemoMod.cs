using Hopper.Core;
using Hopper.Core.Behaviors.Basic;
using Hopper.Core.Items;
using Hopper.Core.Mods;
using Hopper.Core.Registry;
using Hopper.Core.Retouchers;
using Hopper.Core.Targeting;
using Hopper.Utils.Vector;
using Hopper.View;

namespace Hopper
{
    public class DemoMod : IMod
    {
        public EntityFactory<Player> PlayerFactory;
        public EntityFactory<Entity> ChestFactory;
        public EntityFactory<Wall> WallFactory;
        public ModularShovel ShovelItem;
        public ModularWeapon KnifeItem;
        public ModularWeapon SpearItem;

        public int Offset => 200;
        public string Name => "demo";

        public DemoMod()
        {
        }

        private void CreateItems()
        {
            var knifeTargetProvider = TargetProvider.CreateAtk(
                Pattern.Default,
                Handlers.DefaultAtkChain
            );

            ShovelItem = new ModularShovel(
                new ItemMetadata("Base_Shovel"),
                TargetProvider.SimpleDig
            );


            KnifeItem = new ModularWeapon(
                new ItemMetadata("Base_Knife"),
                knifeTargetProvider
            );


            SpearItem = new ModularWeapon(
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
                            reach = new int[] { 1 }
                        }
                    ),
                    Handlers.DefaultAtkChain
                )
            );


            // Reference this item once so that it is added to the registry
            // var _ = Bow.DefaultItem;
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

        public void RegisterSelf(ModSubRegistry registry)
        {
            CreateItems();
            PlayerFactory = CreatePlayerFactory().AddBehavior<Statused>();
            ChestFactory = new EntityFactory<Entity>().AddBehavior<Interactable>(
                new Interactable.Config(new PoolItemContentSpec("zone1/stuff"))
            );
            WallFactory = new EntityFactory<Wall>().AddBehavior<Damageable>();

            ShovelItem.RegisterSelf(registry);
            KnifeItem.RegisterSelf(registry);
            SpearItem.RegisterSelf(registry);

            PlayerFactory.RegisterSelf(registry);
            ChestFactory.RegisterSelf(registry);
            WallFactory.RegisterSelf(registry);

            TileStuff.CreatedEventPath.Event.RegisterSelf(registry);
        }

        public void Patch(Repository repository)
        {
            TileStuff.CreatedEventPath.Event.Patch(repository);
        }

        public void AfterPatch(Repository repository)
        {
            PlayerFactory.AfterPatch(repository);
            ChestFactory.AfterPatch(repository);
            WallFactory.AfterPatch(repository);
        }
    }
}