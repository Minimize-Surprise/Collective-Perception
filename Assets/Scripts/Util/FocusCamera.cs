using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FocusCamera : MonoBehaviour
{
    Camera mainCam;
    Camera focusCam;
    private GameObject mainCamHome;
    
    private bool isFollowing = false;
    private GameObject followBot;
    private BeeClust followBotScript;

    private bool reachedAndFollow = false;

    private float followYAxisFactor = 0.8f;
    
    private float moveSpeed = 10f;
    
    private float minFov = 15f;
    private float maxFov = 90f;
    private float sensitivityZoom = 25f;
    private float initialZoom;
    
    public Text focusInfoText;

    public Text currentArenaText;
    private int currentArena = 0;
    private int nParallelArenas;
    private float arenaWidth;
    private GameObject mainCamCurrHome;

    void Start()
    {
        mainCam = Camera.main;
        mainCamHome = GameObject.Find("Main Camera Home");
        mainCamCurrHome = new GameObject("Main Camera Current Home");
        initialZoom = mainCam.fieldOfView;
        var master = GameObject.Find("Master").GetComponent<Parameters>();
        nParallelArenas = master.nParallelArenas;
        arenaWidth = master.arenaWidth;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleMouseRay();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetZoom();
            if (isFollowing)
                LeaveFollowMode();
        }

        if (isFollowing && reachedAndFollow)
        {
            FollowTargetBotAround();
        }

        UpdateFocusInfoText();

        HandleZoom();

        HandleArenaMovement();

    }

    private void HandleArenaMovement()
    {
        var change = false;
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentArena = Mathf.Min(currentArena + 1, nParallelArenas - 1);
            change = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentArena = Mathf.Max(currentArena - 1, 0);
            change = true;
        }

        if (!change) return;
        currentArenaText.text = $"Current Arena Index: {currentArena}";
        mainCamCurrHome.transform.position = mainCamHome.transform.position +
                                             new Vector3(currentArena * 1.5f * arenaWidth,0,0);    
        StartCoroutine(MoveTowards(transform, mainCamCurrHome.transform.position, 0.5f));
    }

    private void ResetZoom()
    {
        mainCam.fieldOfView = initialZoom;
    }

    void HandleZoom()
    {
        var view = mainCam.ScreenToViewportPoint(Input.mousePosition);
        var isOutside = view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;
        if (isOutside) return;
        
        var fov = mainCam.fieldOfView;
        fov -= Input.GetAxis("Mouse ScrollWheel") * sensitivityZoom;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        mainCam.fieldOfView = fov;
    }

    private void FollowTargetBotAround()
    {
        var mainCamPos = mainCamCurrHome.transform.position;
        var followBotPos = followBot.transform.position;
        Vector3 target = new Vector3(followBotPos.x, isFollowing? mainCamPos.y * followYAxisFactor:mainCamPos.y, followBotPos.z);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);    
    }

    private void HandleMouseRay()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast (ray, out var hit))
        {
            if(hit.transform.CompareTag("MONA"))
            {
                EnterFollowMode(hit.transform);
            }
            else
            {
                LeaveFollowMode();
            }
        }
    }

    private void UpdateFocusInfoText()
    {
        focusInfoText.text = !isFollowing ? "" : 
            "<b>Info of Currently Focussed Bot</b>" + Environment.NewLine + 
        followBotScript.botController.GetFocusInfoText();
    }

    private void LeaveFollowMode()
    {
        if (!isFollowing) return;
        isFollowing = false;
        reachedAndFollow = false;
        followBot = mainCamCurrHome;
        StartCoroutine(MoveTowards(transform, mainCamCurrHome.transform.position, 0.5f));
    }

    private void EnterFollowMode(Transform hitTransform)
    {
        isFollowing = true;
        followBot = hitTransform.gameObject;
        followBotScript = hitTransform.gameObject.GetComponent<BeeClust>();
        Vector3 target = new Vector3(followBot.transform.position.x,
            mainCamCurrHome.transform.position.y * followYAxisFactor, 
            followBot.transform.position.z);
        StartCoroutine(MoveTowards(transform, target, 0.5f));
    }
    
    IEnumerator MoveTowards(Transform objectToMove, Vector3 toPosition, float duration)
    {
        float counter = 0;

        while (counter < duration)
        {
            counter += Time.deltaTime;
            Vector3 currentPos = objectToMove.position;

            float time = Vector3.Distance(currentPos, toPosition) / (duration - counter) * Time.deltaTime;

            objectToMove.position = Vector3.MoveTowards(currentPos, toPosition, time);
            
            yield return null;
        }
        
        if (isFollowing)
            reachedAndFollow = true;
    }
    
}
