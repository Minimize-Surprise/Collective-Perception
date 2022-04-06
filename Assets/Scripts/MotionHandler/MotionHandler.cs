using UnityEngine;

namespace Assets
{
    public enum TurnDirection{
        ClockWise,
        CounterClockWise
    }
    
    public abstract class MotionHandler
    {
        protected BotController botControllerParent;
        protected Parameters master;
        protected GameObject gameObject;

        protected MotionHandler(BotController botControllerParent)
        {
            this.botControllerParent = botControllerParent;
        }

        public virtual void Initialize()
        {
            this.master = botControllerParent.parentScript.masterParams;
            this.gameObject = botControllerParent.parentScript.gameObject;
        }
        
        public virtual void Reinitialize()
        {
        }
        

        public abstract void TurnInDirection(TurnDirection turnDirection);

        public abstract void MoveStraight();
        
        protected bool IsNextPositionInArena(Vector2 nextPosition)
        {
            var xLowerBoundOkay = nextPosition.x > (0 + master.beeWidth / 2);
            var xUpperBoundOkay = nextPosition.x < (master.arenaWidth - master.beeWidth / 2);
            var yLowerBoundOkay = nextPosition.y > (0 + master.beeLength / 2);
            var yUpperBoundOkay = nextPosition.y < (master.arenaHeight - master.beeLength / 2);

            return xLowerBoundOkay && xUpperBoundOkay && yLowerBoundOkay && yUpperBoundOkay;
        }

        protected bool IsNextPositionInArena(Vector3 nextPosition)
        {
            return IsNextPositionInArena(new Vector2(nextPosition.x, nextPosition.z));
        }
        
    }
}