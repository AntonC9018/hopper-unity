using Hopper.Core;
using Hopper.Core.Behaviors;
using Hopper.Core.Items;
using Hopper.Core.Retouchers;
using Hopper.Core.Targeting;
using Hopper.Test_Content;

namespace Hopper
{
    public class Factories
    {
        public EntityFactory<Player> playerFactory;
        public EntityFactory<Wall> wallFactory;
        public EntityFactory<Entity> enemyFactory;
        public EntityFactory<Entity> chestFactory;
        public EntityFactory<Water> waterFactory;
        public EntityFactory<IceFloor> iceFactory;
        public EntityFactory<BounceTrap> trapFactory;
        public EntityFactory<RealBarrier> barrierFactory;

        public Factories(Registry registry, CoreRetouchers retouchers)
        {
            playerFactory = CreatePlayerFactory(retouchers).AddBehavior<Statused>();

            wallFactory = new EntityFactory<Wall>()
                .AddBehavior<Attackable>()
                .Retouch(
                    retouchers.Attackness.Constant(Attackness.NEVER)
                )
                .AddBehavior<Damageable>();

            enemyFactory = Hopper.Test_Content.Skeleton.Factory(retouchers);

            chestFactory = new EntityFactory<Entity>()
                .AddBehavior<Interactable>(
                    new Interactable.Config(new PoolItemContentSpec("zone1/stuff"))
                );

            // waterFactory = Water.CreateFactory();
            // iceFactory = IceFloor.CreateFactory();
            // trapFactory = BounceTrap.CreateFactory();
            barrierFactory = BlockingTrap.BarrierFactory;

            playerFactory.RegisterSelf(registry);
            wallFactory.RegisterSelf(registry);
            enemyFactory.RegisterSelf(registry);
            chestFactory.RegisterSelf(registry);
            // waterFactory.RegisterSelf(registry);
            // iceFactory.RegisterSelf(registry);
            // trapFactory.RegisterSelf(registry);
            barrierFactory.RegisterSelf(registry);
        }

        private EntityFactory<Player> CreatePlayerFactory(CoreRetouchers retouchers)
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

            var playerFactory = Player.CreateFactory(retouchers)
                .AddBehavior<Controllable>(new Controllable.Config
                {
                    defaultAction = defaultAction
                });

            return playerFactory;
        }
    }
}