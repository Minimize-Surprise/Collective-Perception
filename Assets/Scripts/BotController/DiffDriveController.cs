using System;
using Assets;
using MotionStateMachine;
using UnityEngine;

public class DiffDriveController : BotController
{
    [SerializeField]
    private RandomWalkStateMachine motionStateMachine;

    private DecisionStateMachine decisionStateMachine;

    public DiffDriveController(BeeClust parentScript, BotParams botParams) : base(parentScript, botParams)
    {
        motionHandler = new DiffDriveMotionHandler(this);
        SetParameters(botParams);
    }

    public new void SetParameters(BotParams botParams)
    {
        base.SetParameters(botParams);
        this.botParams = botParams;
    }
    
    public override void Initialize()
    {
        base.Initialize();
        motionHandler.Initialize();
        this.motionStateMachine = new RandomWalkStateMachine(this);
        this.decisionStateMachine = new DecisionStateMachine(this);
    }

    public override void Reinitialize(Opinion newOpinion)
    {
        base.Reinitialize(newOpinion);
        this.motionStateMachine.Reinitialize();
        this.decisionStateMachine.Reinitialize();
    }
    
    public override void Step()
    {
        // [SENSOR LOGIC]
        // UpdateSensorVectors(); // not needed anymore - is done & cached automatically
        if (parentScript.masterParams.gizmoManager.drawProximSensorRange)
        {
            DrawSensorRays(true);
        }
        
        this.opinionHandler.Step();
        
        /*
        if (!done)
        {
            for (int i = 0; i < proximitySensors.Length; i++)
            {
                var hit = proximitySensors[i].BinaryRead();
                var dist = proximitySensors[i].DistanceRead();
                Debug.Log($"Proximity sensor at {proximitySensors[i].relativeAngle.ToString("F2")}° - hit: {hit}, dist: {dist}");
            }
            done = true;
        }
        */
        
        // [MOVEMENT LOGIC]
        if (!masterParameters.run) return;

        // > Testing motion wrapper
        //var mh = (DiffDriveMotionHandler) motionHandler;
        //mh.MotionWrapper(2, 2);
        
        motionStateMachine.Step();
        decisionStateMachine.Step();
    }
    
    public override bool IsListening()
    {
        return decisionStateMachine.currentlyListening;
    }

    public override string GetFocusInfoText()
    {
        var msmText = motionStateMachine.GetFocusInfoText();
        var dsmText = decisionStateMachine.GetFocusInfoText();
        var ohText = opinionHandler.GetFocusInfoText();
        
        return String.Join(Environment.NewLine + Environment.NewLine, 
            new string[] {base.GetFocusInfoText(), msmText, dsmText, ohText});;
    }
    
}