using UnityEngine;

namespace Assets
{
    
    public class BeeClustMotionHandler : MotionHandler
    {
        private BeeClustController botController;
        private Transform transform;
        
        private Vector3 nextPosition;
        
        public Quaternion startBearing;
        public float targetRelativeTurnAngle;
        
        public BeeClustMotionHandler(BotController parentBotController) : base(parentBotController)
        {
            botController = (BeeClustController) parentBotController;
        }

        public override void Initialize()
        {
            base.Initialize();
            this.transform = botControllerParent.parentScript.gameObject.transform;
        }

        public void Turn()
        {
            if (targetRelativeTurnAngle < 0)
            {
                gameObject.transform.RotateAround(transform.position, Vector3.up, -botController.turnSpeed * Time.deltaTime);
            }
            else
            {
                gameObject.transform.RotateAround(transform.position, Vector3.up, botController.turnSpeed * Time.deltaTime);
            }

            var targetAngleReached = Mathf.Abs(Quaternion.Angle(startBearing, transform.rotation)) >=
                                     Mathf.Abs(targetRelativeTurnAngle);
            if (targetAngleReached)
            {
                botController.turning = false;
            }
        }

        public override void TurnInDirection(TurnDirection turnDirection)
        {
            if (turnDirection == TurnDirection.CounterClockWise)
            {
                gameObject.transform.RotateAround(transform.position, Vector3.up, -botController.turnSpeed * Time.deltaTime);
            }
            else
            {
                gameObject.transform.RotateAround(transform.position, Vector3.up, botController.turnSpeed * Time.deltaTime);
            }
        }

        public override void MoveStraight()
        {
            nextPosition = transform.position + transform.forward * (botController.forwardSpeed * Time.deltaTime);
        
            if (IsNextPositionInArena(nextPosition))
            {
                transform.position += transform.forward * (botController.forwardSpeed * Time.deltaTime);
            }
            else
            {
                botController.turning = true;
            }
        }

        
    }
}