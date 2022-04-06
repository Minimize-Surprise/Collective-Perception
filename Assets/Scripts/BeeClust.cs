using Assets;
using UnityEngine;

public class BeeClust : MonoBehaviour
{    
    public Parameters masterParams;

    public string pathCollisionLog, pathStateLog;
    public string fileDir;
    
    
    public BotParams botParams;
    
    public BotController botController;

    private void Awake()
    {
        masterParams = GameObject.Find("Master").GetComponent<Parameters>();

        CheckBotParams();
        
        // Need to create a new (subclass) botController and overtake parameters.
        var oldBotController = botController;
        //botController = new BeeClustController(this, botParams);
        botController = new DiffDriveController(this, botParams);
        botController.ApplyParamsFromOldController(oldBotController);
        
        botController.Initialize();
    }

    private void CheckBotParams()
    {
        if (botParams == null)
        {
            botParams = masterParams.botParams;
            Debug.Log("Pulling botParams from master obj", this);
        }
    }
    
    void Start()
    {
        fileDir = masterParams.fileDir;
    }
    
    public void SetBotParams(BotParams botParams)
    {
        this.botParams = botParams;
    }

    private void FixedUpdate()
    {
        botController.Step();
    }

    public void Reinitialize(Vector3 pos, Quaternion rot, Opinion opinion)
    {
        var thisTransform = this.transform;
        thisTransform.localPosition = pos;
        thisTransform.rotation = rot;
        this.botController.Reinitialize(opinion);
    }

    public void ReceiveOpinionMessage(OpinionMessage msg)
    {
        this.botController.opinionHandler.HandleIncomingOpinionMessage(msg);
    }

    public Opinion QueryOpinion()
    {
        return this.botController.opinionHandler.QueryCurrentOpinion();
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (masterParams.gizmoManager.drawCommunicationRange)
            transform.DrawGizmoDisk(masterParams.communicationRange);

    }
    
}


