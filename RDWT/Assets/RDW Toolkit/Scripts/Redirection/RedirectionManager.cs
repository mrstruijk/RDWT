using System;
using Redirection;
using UnityEngine;


public class RedirectionManager : MonoBehaviour
{
    public enum MovementController
    {
        Keyboard,
        AutoPilot,
        Tracker
    }


    [Tooltip("Select if you wish to run simulation from commandline in Unity batchmode.")]
    public bool runInTestMode = false;

    [Tooltip("How user movement is controlled.")]
    public MovementController Controller = MovementController.Tracker;

    [Tooltip("Maximum translation gain applied")]
    [Range(0, 5)]
    public float MaxTransGain = 0.26F;

    [Tooltip("Minimum translation gain applied")]
    [Range(-0.99F, 0)]
    public float MinTransGain = -0.14F;

    [Tooltip("Maximum rotation gain applied")]
    [Range(0, 5)]
    public float MaxRotGain = 0.49F;

    [Tooltip("Minimum rotation gain applied")]
    [Range(-0.99F, 0)]
    public float MinRotGain = -0.2F;

    [Tooltip("Radius applied by curvature gain")]
    [Range(1, 23)]
    public float CurvatureRadius = 7.5F;

    [Tooltip("The game object that is being physically tracked (probably user's head)")]
    public Transform headTransform;

    [Tooltip("Use simulated framerate in auto-pilot mode")]
    public bool useManualTime = false;

    [Tooltip("Target simulated framerate in auto-pilot mode")]
    public float targetFPS = 60;


    [HideInInspector]
    public Transform body;
    [HideInInspector]
    public Transform trackedSpace;
    [HideInInspector]
    public Transform simulatedHead;

    [HideInInspector]
    public Redirector redirector;
    [HideInInspector]
    public Resetter resetter;
    [HideInInspector]
    public ResetTrigger resetTrigger;
    [HideInInspector]
    public TrailDrawer trailDrawer;
    [HideInInspector]
    public SimulationManager simulationManager;
    [HideInInspector]
    public SimulatedWalker simulatedWalker;
    [HideInInspector]
    public KeyboardController keyboardController;
    [HideInInspector]
    public SnapshotGenerator snapshotGenerator;
    [HideInInspector]
    public StatisticsLogger statisticsLogger;
    [HideInInspector]
    public HeadFollower bodyHeadFollower;

    [HideInInspector]
    public Vector3 currPos, currPosReal, prevPos, prevPosReal;
    [HideInInspector]
    public Vector3 currDir, currDirReal, prevDir, prevDirReal;
    [HideInInspector]
    public Vector3 deltaPos;
    [HideInInspector]
    public float deltaDir;
    [HideInInspector]
    public Transform targetWaypoint;


    [HideInInspector]
    public bool inReset = false;

    [HideInInspector]
    public string startTimeOfProgram;

    private float _simulatedTime = 0;


    private void Awake()
    {
        startTimeOfProgram = DateTime.Now.ToString("yyyy MM dd HH:mm:ss");

        GetBody();
        GetTrackedSpace();
        GetSimulatedHead();

        GetSimulationManager();
        SetReferenceForSimulationManager();
        simulationManager.Initialize();

        GetRedirector();
        GetResetter();
        GetResetTrigger();
        GetTrailDrawer();

        GetSimulatedWalker();
        GetKeyboardController();
        GetSnapshotGenerator();
        GetStatisticsLogger();
        GetBodyHeadFollower();
        SetReferenceForRedirector();
        SetReferenceForResetter();
        SetReferenceForResetTrigger();
        SetBodyReferenceForResetTrigger();
        SetReferenceForTrailDrawer();

        SetReferenceForSimulatedWalker();
        SetReferenceForKeyboardController();
        SetReferenceForSnapshotGenerator();
        SetReferenceForStatisticsLogger();
        SetReferenceForBodyHeadFollower();

        // The rule is to have RedirectionManager call all "Awake"-like functions that rely on RedirectionManager as an "Initialize" call.
        resetTrigger.Initialize();

        // Resetter needs ResetTrigger to be initialized before initializing itself
        if (resetter != null)
        {
            resetter.Initialize();
        }

        if (runInTestMode)
        {
            Controller = MovementController.AutoPilot;
        }

        if (Controller != MovementController.Tracker)
        {
            headTransform = simulatedHead;
        }
    }


    // Use this for initialization
    private void Start()
    {
        _simulatedTime = 0;
        UpdatePreviousUserState();
    }


    // Update is called once per frame
    private void Update()
    {
    }


    private void LateUpdate()
    {
        _simulatedTime += 1.0f / targetFPS;

        //if (MOVEMENT_CONTROLLER == MovementController.AutoPilot)
        //    simulatedWalker.WalkUpdate();

        UpdateCurrentUserState();
        CalculateStateChanges();

        // BACK UP IN CASE UNITY TRIGGERS FAILED TO COMMUNICATE RESET (Can happen in high speed simulations)
        if (resetter != null && !inReset && resetter.IsUserOutOfBounds())
        {
            Debug.LogWarning("Reset Aid Helped!");
            OnResetTrigger();
        }

        if (inReset)
        {
            if (resetter != null)
            {
                resetter.ApplyResetting();
            }
        }
        else
        {
            if (redirector != null)
            {
                redirector.ApplyRedirection();
            }
        }

        statisticsLogger.UpdateStats();

        UpdatePreviousUserState();

        UpdateBodyPose();
    }


    public float GetDeltaTime()
    {
        if (useManualTime)
        {
            return 1.0f / targetFPS;
        }

        return Time.deltaTime;
    }


    public float GetTime()
    {
        if (useManualTime)
        {
            return _simulatedTime;
        }

        return Time.time;
    }


    private void UpdateBodyPose()
    {
        body.position = Utilities.FlattenedPos3D(headTransform.position);
        body.rotation = Quaternion.LookRotation(Utilities.FlattenedDir3D(headTransform.forward), Vector3.up);
    }


    private void SetReferenceForRedirector()
    {
        if (redirector != null)
        {
            redirector.RedirectionManager = this;
        }
    }


    private void SetReferenceForResetter()
    {
        if (resetter != null)
        {
            resetter.redirectionManager = this;
        }
    }


    private void SetReferenceForResetTrigger()
    {
        if (resetTrigger != null)
        {
            resetTrigger.redirectionManager = this;
        }
    }


    private void SetBodyReferenceForResetTrigger()
    {
        if (resetTrigger != null && body != null)
        {
            // NOTE: This requires that getBody gets called before this
            resetTrigger.bodyCollider = body.GetComponentInChildren<CapsuleCollider>();
        }
    }


    private void SetReferenceForTrailDrawer()
    {
        if (trailDrawer != null)
        {
            trailDrawer.redirectionManager = this;
        }
    }


    private void SetReferenceForSimulationManager()
    {
        if (simulationManager != null)
        {
            simulationManager.redirectionManager = this;
        }
    }


    private void SetReferenceForSimulatedWalker()
    {
        if (simulatedWalker != null)
        {
            simulatedWalker.redirectionManager = this;
        }
    }


    private void SetReferenceForKeyboardController()
    {
        if (keyboardController != null)
        {
            keyboardController.redirectionManager = this;
        }
    }


    private void SetReferenceForSnapshotGenerator()
    {
        if (snapshotGenerator != null)
        {
            snapshotGenerator.redirectionManager = this;
        }
    }


    private void SetReferenceForStatisticsLogger()
    {
        if (statisticsLogger != null)
        {
            statisticsLogger.redirectionManager = this;
        }
    }


    private void SetReferenceForBodyHeadFollower()
    {
        if (bodyHeadFollower != null)
        {
            bodyHeadFollower.redirectionManager = this;
        }
    }


    private void GetRedirector()
    {
        redirector = gameObject.GetComponent<Redirector>();

        if (redirector == null)
        {
            gameObject.AddComponent<NullRedirector>();
        }

        redirector = gameObject.GetComponent<Redirector>();
    }


    private void GetResetter()
    {
        resetter = gameObject.GetComponent<Resetter>();

        if (resetter == null)
        {
            gameObject.AddComponent<NullResetter>();
        }

        resetter = gameObject.GetComponent<Resetter>();
    }


    private void GetResetTrigger()
    {
        resetTrigger = gameObject.GetComponentInChildren<ResetTrigger>();
    }


    private void GetTrailDrawer()
    {
        trailDrawer = gameObject.GetComponent<TrailDrawer>();
    }


    private void GetSimulationManager()
    {
        simulationManager = gameObject.GetComponent<SimulationManager>();
    }


    private void GetSimulatedWalker()
    {
        simulatedWalker = simulatedHead.GetComponent<SimulatedWalker>();
    }


    private void GetKeyboardController()
    {
        keyboardController = simulatedHead.GetComponent<KeyboardController>();
    }


    private void GetSnapshotGenerator()
    {
        snapshotGenerator = gameObject.GetComponent<SnapshotGenerator>();
    }


    private void GetStatisticsLogger()
    {
        statisticsLogger = gameObject.GetComponent<StatisticsLogger>();
    }


    private void GetBodyHeadFollower()
    {
        bodyHeadFollower = body.GetComponent<HeadFollower>();
    }


    private void GetBody()
    {
        body = transform.Find("Body");
    }


    private void GetTrackedSpace()
    {
        trackedSpace = transform.Find("Tracked Space");
    }


    private void GetSimulatedHead()
    {
        simulatedHead = transform.Find("Simulated User").Find("Head");
    }


    private void GetTargetWaypoint()
    {
        targetWaypoint = transform.Find("Target Waypoint").gameObject.transform;
    }


    private void UpdateCurrentUserState()
    {
        currPos = Utilities.FlattenedPos3D(headTransform.position);
        currPosReal = Utilities.GetRelativePosition(currPos, transform);
        currDir = Utilities.FlattenedDir3D(headTransform.forward);
        currDirReal = Utilities.FlattenedDir3D(Utilities.GetRelativeDirection(currDir, transform));
    }


    private void UpdatePreviousUserState()
    {
        prevPos = Utilities.FlattenedPos3D(headTransform.position);
        prevPosReal = Utilities.GetRelativePosition(prevPos, transform);
        prevDir = Utilities.FlattenedDir3D(headTransform.forward);
        prevDirReal = Utilities.FlattenedDir3D(Utilities.GetRelativeDirection(prevDir, transform));
    }


    private void CalculateStateChanges()
    {
        deltaPos = currPos - prevPos;
        deltaDir = Utilities.GetSignedAngle(prevDir, currDir);
    }


    public void OnResetTrigger()
    {
        //print("RESET TRIGGER");
        if (inReset)
        {
            return;
        }

        //print("NOT IN RESET");
        //print("Is Resetter Null? " + (resetter == null));
        if (resetter != null && resetter.IsResetRequired())
        {
            //print("RESET WAS REQUIRED");
            resetter.InitializeReset();
            inReset = true;
        }
    }


    public void OnResetEnd()
    {
        //print("RESET END");
        resetter.FinalizeReset();
        inReset = false;
    }


    public void RemoveRedirector()
    {
        redirector = gameObject.GetComponent<Redirector>();

        if (redirector != null)
        {
            Destroy(redirector);
        }

        redirector = null;
    }


    public void UpdateRedirector(Type redirectorType)
    {
        RemoveRedirector();
        redirector = (Redirector) gameObject.AddComponent(redirectorType);
        //this.redirector = this.gameObject.GetComponent<Redirector>();
        SetReferenceForRedirector();
    }


    public void RemoveResetter()
    {
        resetter = gameObject.GetComponent<Resetter>();

        if (resetter != null)
        {
            Destroy(resetter);
        }

        resetter = null;
    }


    public void UpdateResetter(Type resetterType)
    {
        RemoveResetter();

        if (resetterType != null)
        {
            resetter = (Resetter) gameObject.AddComponent(resetterType);
            //this.resetter = this.gameObject.GetComponent<Resetter>();
            SetReferenceForResetter();

            if (resetter != null)
            {
                resetter.Initialize();
            }
        }
    }


    public void UpdateTrackedSpaceDimensions(float x, float z)
    {
        trackedSpace.localScale = new Vector3(x, 1, z);
        resetTrigger.Initialize();

        if (resetter != null)
        {
            resetter.Initialize();
        }
    }
}