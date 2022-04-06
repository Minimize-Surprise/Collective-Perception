using Assets;

[System.Serializable]
public class BotParams
{
    public float maxSensingRange;
    public float forwardSpeed;
    public float turnSpeed;
    public int botIndex;
    public int homeArena;
    
    // BeeClustParams
    public float maxW;
    public float theta;

    
    // Opinion & Collective Perception params
    public Opinion initialOpinion;
    public int maxMsgQueueSize;
    public float g;
    public float sigma;
    
    public BotParams(float maxSensingRange, float forwardSpeed, float turnSpeed, int botIndex, int homeArena)
    {
        this.maxSensingRange = maxSensingRange;
        this.forwardSpeed = forwardSpeed;
        this.turnSpeed = turnSpeed;
        this.botIndex = botIndex;
        this.homeArena = homeArena;
    }

    public void SetBeeClustParams(float maxW, float theta)
    {
        this.maxW = maxW;
        this.theta = theta;
    }

    public void SetOpinionParams(Opinion initialOpinion, int maxMsgQueueSize, float g, float sigma)
    {
        this.initialOpinion = initialOpinion;
        this.maxMsgQueueSize = maxMsgQueueSize;
        this.g = g;
        this.sigma = sigma;
    }
}
