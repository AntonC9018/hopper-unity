using Core;
using Core.Behaviors;
using Core.Items;
using Core.Targeting;
using Test;

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

        public Factories()
        {
            playerFactory = CreatePlayerFactory().AddBehavior<Statused>();

            wallFactory = new EntityFactory<Wall>()
                .AddBehavior<Attackable>()
                .Retouch(
                    Core.Retouchers.Attackableness.Constant(Attackness.NEVER)
                )
                .AddBehavior<Damageable>();

            enemyFactory = Test.Skeleton.Factory;

            chestFactory = new EntityFactory<Entity>()
                .AddBehavior<Interactable>(
                    new Interactable.Config(new PoolItemContentSpec("zone1/stuff"))
                );

            waterFactory = Water.CreateFactory();
            iceFactory = IceFloor.CreateFactory();
            trapFactory = BounceTrap.Factory;
            barrierFactory = BlockingTrap.BarrierFactory;
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
    }
}