using System;
using System.Collections.Generic;
using Redirection;
using UnityEngine;
using Random = UnityEngine.Random;


public class SimulationManager : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;

    [SerializeField] private bool runInSimulationMode = false;

    [SerializeField] private AlgorithmChoice condAlgorithm;

    [SerializeField] private ResetChoice condReset;

    [SerializeField] private PathSeedChoice condPath;

    [SerializeField] private ExperimentChoice condExperiment;


    [SerializeField] private float MAX_TRIALS = 10f;

    [SerializeField] private bool runAtFullSpeed = false;
    [SerializeField]
    public bool onlyRandomizeForward = true;
    [SerializeField] private bool averageTrialResults = false;
    [SerializeField]
    public float DISTANCE_TO_WAYPOINT_THRESHOLD = 0.3f; // Maximum distance requirement to trigger waypoint
    [HideInInspector]
    public bool experimentInProgress = false;
    [HideInInspector]
    public List<Vector2> waypoints;
    [HideInInspector]
    public int waypointIterator = 0;
    [HideInInspector]
    public bool userIsWalking = false;
    private bool _experimentComplete = false;
    private int _experimentIterator = 0;

    private List<ExperimentSetup> _experimentSetups;
    private float _framesInExperiment = 0;
    private List<Vector3> _gainScaleFactors = new();
    private List<InitialConfiguration> _initialConfigurations = new();
    private List<VirtualPathGenerator.PathSeed> _pathSeeds = new();

    // Experiment Variables
    private Type _redirector = null;
    private Type _resetter = null;

    private bool _takeScreenshot = false;
    private List<TrackingSizeShape> _trackingSizes = new();

    private float _trialsForCurrentExperiment = 5;
    private readonly float _zagAngle = 140;

    private readonly float _zigLength = 5.5f;
    private readonly int _zigzagWaypointCount = 6;

    //[SerializeField]
    //bool showUserStartAndEndInLastSnapshot;

    [HideInInspector]
    public static string CommandLineRunCode = "";


    private VirtualPathGenerator.PathSeed GetPathSeedOfficeBuilding()
    {
        var distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 2, 8);
        var angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 90, 90, VirtualPathGenerator.AlternationType.Random);
        var waypointCount = 200;

        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }


    private VirtualPathGenerator.PathSeed GetPathSeedZigzag()
    {
        var distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, _zigLength, _zigLength);
        var angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, _zagAngle, _zagAngle, VirtualPathGenerator.AlternationType.Constant);
        var waypointCount = _zigzagWaypointCount;

        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }


    private VirtualPathGenerator.PathSeed GetPathSeedExplorationSmall()
    {
        var distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 2, 6);
        var angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, -180, 180);
        var waypointCount = 250;

        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }


    private VirtualPathGenerator.PathSeed GetPathSeedExplorationLarge()
    {
        var distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 8, 12);
        var angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, -180, 180);
        var waypointCount = 100;

        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }


    private VirtualPathGenerator.PathSeed GetPathSeedLongCorridor()
    {
        var distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 1000, 1000);
        var angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 0, 0);
        var waypointCount = 1;

        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }


    private void SetUpExperimentFixedTrackingArea(PathSeedChoice pathSeedChoice, Type redirector, Type resetter)
    {
        // Initialize Values
        _redirector = redirector;
        _resetter = resetter;
        _pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        _trackingSizes = new List<TrackingSizeShape>();
        _initialConfigurations = new List<InitialConfiguration>();
        _gainScaleFactors = new List<Vector3>();
        _trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                _pathSeeds.Add(GetPathSeedOfficeBuilding());

                break;
            case PathSeedChoice.ExplorationSmall:
                _pathSeeds.Add(GetPathSeedExplorationSmall());

                break;
            case PathSeedChoice.ExplorationLarge:
                _pathSeeds.Add(GetPathSeedExplorationLarge());

                break;
            case PathSeedChoice.LongWalk:
                _pathSeeds.Add(GetPathSeedLongCorridor());

                break;
            case PathSeedChoice.ZigZag:
                _pathSeeds.Add(GetPathSeedZigzag());

                break;
        }

        _trackingSizes.Add(new TrackingSizeShape(redirectionManager.trackedSpace.localScale.x, redirectionManager.trackedSpace.localScale.z));

        _initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        _gainScaleFactors.Add(Vector3.one);
    }


    private void SetUpExperimentTrackingAreaSizePerformance(PathSeedChoice pathSeedChoice, Type redirector, Type resetter)
    {
        // Initialize Values
        _redirector = redirector;
        _resetter = resetter;
        _pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        _trackingSizes = new List<TrackingSizeShape>();
        _initialConfigurations = new List<InitialConfiguration>();
        _gainScaleFactors = new List<Vector3>();
        _trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                _pathSeeds.Add(GetPathSeedOfficeBuilding());

                break;
            case PathSeedChoice.ExplorationSmall:
                _pathSeeds.Add(GetPathSeedExplorationSmall());

                break;
            case PathSeedChoice.ExplorationLarge:
                _pathSeeds.Add(GetPathSeedExplorationLarge());

                break;
            case PathSeedChoice.LongWalk:
                _pathSeeds.Add(GetPathSeedLongCorridor());

                break;
            case PathSeedChoice.ZigZag:
                _pathSeeds.Add(GetPathSeedZigzag());

                break;
        }

        for (var i = 2; i <= 60; i += 1)
        {
            _trackingSizes.Add(new TrackingSizeShape(i, i));
        }

        _initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        _gainScaleFactors.Add(Vector3.one);
    }


    private void SetUpExperimentTrackingAreaShape(PathSeedChoice pathSeedChoice, Type redirector, Type resetter)
    {
        // Initialize Values
        _redirector = redirector;
        _resetter = resetter;
        _pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        _trackingSizes = new List<TrackingSizeShape>();
        _initialConfigurations = new List<InitialConfiguration>();
        _gainScaleFactors = new List<Vector3>();
        _trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                _pathSeeds.Add(GetPathSeedOfficeBuilding());

                break;
            case PathSeedChoice.ExplorationSmall:
                _pathSeeds.Add(GetPathSeedExplorationSmall());

                break;
            case PathSeedChoice.ExplorationLarge:
                _pathSeeds.Add(GetPathSeedExplorationLarge());

                break;
            case PathSeedChoice.LongWalk:
                _pathSeeds.Add(GetPathSeedLongCorridor());

                break;
            case PathSeedChoice.ZigZag:
                _pathSeeds.Add(GetPathSeedZigzag());

                break;
        }

        for (var area = 100; area <= 200; area += 50)
        {
            for (float ratio = 1; ratio <= 2; ratio += 0.5f)
            {
                _trackingSizes.Add(new TrackingSizeShape(Mathf.Sqrt(area) / Mathf.Sqrt(ratio), Mathf.Sqrt(area) * Mathf.Sqrt(ratio)));
            }
        }

        _initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        _initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(1, 0)));
        _initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), Vector2.one)); // HACK: THIS NON-NORMALIZED ORIENTATION WILL INDICATE DIAGONAL AND WILL BE FIXED LATER
        _gainScaleFactors.Add(Vector3.one);
    }


    /*
    void setUpExperimentGainFactors(PathSeedChoice pathSeedChoice, List<Redirector> redirectors, List<Resetter> resetters)
    {
        // Initialize Values
        this.redirectors = redirectors;
        this.resetters = resetters;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        TRIALS_PER_EXPERIMENT = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
        }

        trackingSizes.Add(new TrackingSizeShape(10, 10));

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));

        for (float g_t = 0; g_t <= 1.5f; g_t += 0.5f)
        {
            for (float g_r = 0; g_r <= 1.5f; g_r += 0.5f)
            {
                for (float g_c = 0; g_c <= 1.5f; g_c += 0.5f)
                {
                    gainScaleFactors.Add(new Vector3(g_t, g_r, g_c));
                }
            }
        }
    }
    */


    private void GenerateAllExperimentSetups()
    {
        // Here we generate the correspondign experiments
        _experimentSetups = new List<ExperimentSetup>();

        foreach (var pathSeed in _pathSeeds)
        {
            foreach (var trackingSize in _trackingSizes)
            {
                foreach (var initialConfiguration in _initialConfigurations)
                {
                    foreach (var gainScaleFactor in _gainScaleFactors)
                    {
                        for (var i = 0; i < _trialsForCurrentExperiment; i++)
                        {
                            _experimentSetups.Add(new ExperimentSetup(_redirector, _resetter, pathSeed, trackingSize, initialConfiguration, gainScaleFactor));
                        }
                    }
                }
            }
        }
    }


    private void StartNextExperiment()
    {
        Debug.Log("---------- EXPERIMENT STARTED ----------");

        var setup = _experimentSetups[_experimentIterator];

        PrintExperimentDescriptor(GetExperimentDescriptor(setup));

        // Setting Gain Scale Factors
        //RedirectionManager.SCALE_G_T = setup.gainScaleFactor.x;
        //RedirectionManager.SCALE_G_R = setup.gainScaleFactor.y;
        //RedirectionManager.SCALE_G_C = setup.gainScaleFactor.z;

        // Enabling/Disabling Redirectors
        redirectionManager.UpdateRedirector(setup.Redirector);
        redirectionManager.UpdateResetter(setup.Resetter);

        // Setup Trail Drawing
        redirectionManager.trailDrawer.enabled = !runAtFullSpeed;

        // Enable User Rendering
        SetUserBodyVisibility(true);

        // Enable Waypoint
        redirectionManager.targetWaypoint.gameObject.SetActive(true);

        // Resetting User and World Positions and Orientations
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        // ESSENTIAL BUG FOUND: If you set the user first and then the redirection recipient, then the user will be moved, so you have to make sure to do it afterwards!
        //Debug.Log("Target User Position: " + setup.initialConfiguration.initialPosition.ToString("f4"));
        redirectionManager.headTransform.position = Utilities.UnFlatten(setup.InitialConfiguration.InitialPosition, redirectionManager.headTransform.position.y);
        //Debug.Log("Result User Position: " + redirectionManager.userHeadTransform.transform.position.ToString("f4"));
        redirectionManager.headTransform.rotation = Quaternion.LookRotation(Utilities.UnFlatten(setup.InitialConfiguration.InitialForward), Vector3.up);

        // Set up Tracking Area Dimensions
        redirectionManager.UpdateTrackedSpaceDimensions(setup.TrackingSizeShape.X, setup.TrackingSizeShape.Z);

        // Adjust Top View Camera Size
        AdjustCameraSizes();
        AdjustTrailWidth();

        // Adjust Screenshot Generator Dimensions
        AdjustSnapshotGeneratorDimensions();

        // Set up Virtual Path
        float sumOfDistances, sumOfRotations;
        waypoints = VirtualPathGenerator.GeneratePath(setup.PathSeed, setup.InitialConfiguration.InitialPosition, setup.InitialConfiguration.InitialForward, out sumOfDistances, out sumOfRotations);
        Debug.Log("sumOfDistances: " + sumOfDistances);
        Debug.Log("sumOfRotations: " + sumOfRotations);

        if (setup.Redirector == typeof(ZigZagRedirector))
        {
            // Create Fake POIs
            var poiRoot = new GameObject().transform;
            poiRoot.name = "ZigZag Redirector Waypoints";
            poiRoot.localPosition = Vector3.zero;
            poiRoot.localRotation = Quaternion.identity;
            var poi0 = new GameObject().transform;
            poi0.localPosition = Vector3.zero;
            poi0.parent = poiRoot;
            var zigzagRedirectorWaypoints = new List<Transform>();
            zigzagRedirectorWaypoints.Add(poi0);

            foreach (var waypoint in waypoints)
            {
                var poi = new GameObject().transform;
                poi.localPosition = Utilities.UnFlatten(waypoint);
                poi.parent = poiRoot;
                zigzagRedirectorWaypoints.Add(poi);
            }

            ((ZigZagRedirector) redirectionManager.redirector).waypoints = zigzagRedirectorWaypoints;
        }

        // NO LONGER SUPPORTING DRAWING FULL VIRTUAL PATH AT BEGINNING
        //if (drawVirtualPath)
        //    virtualPath = redirectionManager.realTrailDrawer.drawPath(setup.initialConfiguration.initialPosition, waypoints, virtualPathColor, null);

        // Set First Waypoint Position and Enable It
        redirectionManager.targetWaypoint.position = new Vector3(waypoints[0].x, redirectionManager.targetWaypoint.position.y, waypoints[0].y);
        waypointIterator = 0;

        // POSTPONING THESE FOR SAFETY REASONS!
        //// Allow Walking
        //UserController.allowWalking = true;

        //// Start Logging
        //redirectionManager.redirectionStatistics.beginLogging();
        //redirectionManager.statisticsLogger.beginLogging();

        //lastExperimentRealStartTime = Time.realtimeSinceStartup;
        experimentInProgress = true;
    }


    private void EndExperiment()
    {
        //Debug.LogWarning("Last Experiment Length: " + (Time.realtimeSinceStartup - lastExperimentRealStartTime));

        var setup = _experimentSetups[_experimentIterator];

        // Stop Trail Drawing
        redirectionManager.trailDrawer.enabled = false;

        // Delete Virtual Path
        // THIS CAN BE MADE OPTIONAL IF NECESSARY
        redirectionManager.trailDrawer.ClearTrail(TrailDrawer.VirtualTrailName);

        // Disable User Rendering
        SetUserBodyVisibility(false);

        // Disable Waypoint
        redirectionManager.targetWaypoint.gameObject.SetActive(true);

        // Disallow Walking
        userIsWalking = false;

        // Stop Logging
        redirectionManager.statisticsLogger.EndLogging();

        // Gather Summary Statistics
        redirectionManager.statisticsLogger.ExperimentResults.Add(redirectionManager.statisticsLogger.GetExperimentResultForSummaryStatistics(GetExperimentDescriptor(setup)));

        // Log Sampled Metrics
        if (redirectionManager.statisticsLogger.logSampleVariables)
        {
            Dictionary<string, List<float>> oneDimensionalSamples;
            Dictionary<string, List<Vector2>> twoDimensionalSamples;
            redirectionManager.statisticsLogger.GetExperimentResultsForSampledVariables(out oneDimensionalSamples, out twoDimensionalSamples);
            redirectionManager.statisticsLogger.LogAllExperimentSamples(ExperimentDescriptorToString(GetExperimentDescriptor(setup)), oneDimensionalSamples, twoDimensionalSamples);
        }

        // Take Snapshot In Next Frame (After User and Virtual Path Is Disabled)
        if (!runAtFullSpeed)
        {
            _takeScreenshot = true;
        }

        // Show User Beging and End
        // We are doing this hackingly by abusing the user and waypoint's default color
        //if (showUserStartAndEndInLastSnapshot)
        //{
        //    // Place User Body At End Point (Becuase of Red) (Already There By Default)
        //    redirectionManager.userBody.gameObject.SetActive(true);
        //    redirectionManager.userOrientationIndicator.gameObject.SetActive(false);
        //    // Place Waypoint At Initial Position
        //    redirectionManager.getNextWaypointTransform().gameObject.SetActive(true);
        //    Vector3 waypointPosition = redirectionManager.simulatedFreezeReset.trackingAreaCoordsToWorldCoordsForPosition(setup.initialConfiguration.initialPosition);
        //    redirectionManager.getNextWaypointTransform().position = new Vector3(waypointPosition.x, redirectionManager.getNextWaypointTransform().position.y, waypointPosition.z);
        //}

        // Prepared for new experiment
        _experimentIterator++;
        //lastExperimentEndTime = Time.time;
        experimentInProgress = false;

        // Log All Summary Statistics To File
        if (_experimentIterator == _experimentSetups.Count)
        {
            if (averageTrialResults)
            {
                redirectionManager.statisticsLogger.ExperimentResults = MergeTrialSummaryStatistics(redirectionManager.statisticsLogger.ExperimentResults);
            }

            //redirectionManager.statisticsLogger.LogExperimentSummaryStatisticsResults(redirectionManager.statisticsLogger.experimentResults);
            redirectionManager.statisticsLogger.LogExperimentSummaryStatisticsResultsScsv(redirectionManager.statisticsLogger.ExperimentResults);
            Debug.Log("Last Experiment Complete");
            _experimentComplete = true;

            if (redirectionManager.runInTestMode)
            {
                Application.Quit();
            }
        }

        // Disabling Redirectors
        redirectionManager.RemoveRedirector();
        redirectionManager.RemoveResetter();
    }


    private void InstantiateSimulationPrefab()
    {
        var waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Destroy(waypoint.GetComponent<SphereCollider>());
        redirectionManager.targetWaypoint = waypoint;
        waypoint.name = "Simulated Waypoint";
        waypoint.position = 1.2f * Vector3.up + 1000 * Vector3.forward;
        waypoint.localScale = 0.3f * Vector3.one;
        waypoint.GetComponent<Renderer>().material.color = new Color(0, 1, 0);
        waypoint.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, 0.12f, 0));
    }


    public void Initialize()
    {
        redirectionManager.runInTestMode = runInSimulationMode;
        userIsWalking = !(redirectionManager.Controller == RedirectionManager.MovementController.AutoPilot);

        if (redirectionManager.Controller == RedirectionManager.MovementController.AutoPilot)
        {
            DISTANCE_TO_WAYPOINT_THRESHOLD = 0.05f; // 0.0001f;
        }

        if (redirectionManager.Controller != RedirectionManager.MovementController.Tracker)
        {
            InstantiateSimulationPrefab();
        }

        if (redirectionManager.Controller == RedirectionManager.MovementController.Tracker)
        {
            return;
        }

        //redirectionManager.simulationDataLogger.generateBatchFiles();

        // Read From Command Line
        //if (redirectionManager.runInTestMode)
        //{
        //    if (System.Environment.GetCommandLineArgs().Length > 1)
        //    {
        //        commandLineRunCode = System.Environment.GetCommandLineArgs()[1].Substring(0, 1) == "-" ? System.Environment.GetCommandLineArgs()[2] : System.Environment.GetCommandLineArgs()[1];
        //        Debug.Log("Run Code: " + commandLineRunCode);
        //    }
        //    else
        //        redirectionManager.runInTestMode = false;
        //}

        // Setting Random Seed
        Random.seed = VirtualPathGenerator.RandomSeed;

        // Make sure VSync doesn't slow us down

        //Debug.Log("Application.targetFrameRate: " + Application.targetFrameRate);

        if (runAtFullSpeed && enabled)
        {
            //redirectionManager.topViewCamera.enabled = false;
            //drawVirtualPath = false;
            QualitySettings.vSyncCount = 0;
        }

        // Also Determine Time Scale
        //if (this.enabled)
        //{
        //    //Time.timeScale = timeScale;
        //    //Time.fixedDeltaTime *= timeScale;
        //}

        // Initialization
        _experimentIterator = 0;
        //if (this.enabled)
        //    redirectionManager.userMovementManager.activateSimulatedWalker();

        /*
        // Here we manually determine what we want to run
        //algorithms.Add(AlgorithmChoice.GreedyTransGain);
        //algorithms.Add(AlgorithmChoice.CenterBased);
        //algorithms.Add(AlgorithmChoice.None);
        algorithms.Add(AlgorithmChoice.S2C);
        //algorithms.Add(AlgorithmChoice.S2O);
        
        //pathSeeds.Add(new SimulationPathGenerator.PathSeed(new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, false, 100, 100), new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, true, 0, 0), 1));
        pathSeeds.Add(new SimulationPathGenerator.PathSeed(new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, false, 1000, 1000), new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, true, 0, 0), 1));

        //SimulationPathGenerator.SamplingDistribution distanceDistribution = new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, false, 8, 10);
        //SimulationPathGenerator.SamplingDistribution angleDistribution = new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, true, Mathf.PI / 4, 3 * Mathf.PI / 4);
        //pathSeeds.Add(new SimulationPathGenerator.PathSeed(distanceDistribution, angleDistribution, 1));

        //for (int i = 2; i <= 30; i += 2)
        //{
        //    trackingSizes.Add(new TrackingSizeShape(i, i));
        //}

        //trackingSizes.Add(new TrackingSizeShape(20, 20));
        //trackingSizes.Add(new TrackingSizeShape(5f, 1.25f));
        //trackingSizes.Add(new TrackingSizeShape(20, 5));
        //trackingSizes.Add(new TrackingSizeShape(100, 20));
        //trackingSizes.Add(new TrackingSizeShape(5, 5));
        //trackingSizes.Add(new TrackingSizeShape(5, 20));
        //trackingSizes.Add(new TrackingSizeShape(10, 10));
        trackingSizes.Add(new TrackingSizeShape(32, 32));
        //trackingSizes.Add(new TrackingSizeShape(50, 50));
        //trackingSizes.Add(new TrackingSizeShape(8, 8));
        //trackingSizes.Add(new TrackingSizeShape(10, 10));

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        //initialConfigurations.Add(new InitialConfiguration(new Vector2(7.5f, 0), new Vector2(0, 1)));
        //initialConfigurations.Add(new InitialConfiguration(true)); // Random Config
        //initialConfigurations.Add(new InitialConfiguration(new Vector2(40, -40), new Vector2(0, 1)));
        */

        //// MANUAL TESTING
        //redirectionManager.runInTestMode = true;
        //commandLineRunCode = "1421";
        //commandLineRunCode = "2531";
        //commandLineRunCode = "242";
        //commandLineRunCode = "244";


        if (redirectionManager.runInTestMode)
        {
            //print("EXP SETUP");
            //int expCode = int.Parse(commandLineRunCode.Substring(0, 1));
            //int pathCode = int.Parse(commandLineRunCode.Substring(1, 1));
            //int algoCode = int.Parse(commandLineRunCode.Substring(2, 1));
            //int resetCode = int.Parse(commandLineRunCode.Substring(3, 1));


            Type redirectorType = null;
            Type resetterType = null;

            switch (condAlgorithm)
            {
                case AlgorithmChoice.None:
                    redirectorType = typeof(NullRedirector);

                    break;
                case AlgorithmChoice.S2C:
                    redirectorType = typeof(S2CRedirector);

                    break;
                case AlgorithmChoice.S2O:
                    redirectorType = typeof(S2ORedirector);

                    break;
                case AlgorithmChoice.Zigzag:
                    redirectorType = typeof(ZigZagRedirector);

                    break;
                //case 4:
                //    algorithmChoice = AlgorithmChoice.CenterBasedTransGainSpeedUp;
                //    break;
                //case 5:
                //    algorithmChoice = AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp;
                //    break;
                //case 6:
                //    algorithmChoice = AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp;
                //    break;
            }

            switch (condReset)
            {
                case ResetChoice.None:
                    resetterType = typeof(NullResetter);

                    break;
                case ResetChoice.TwoOneTurn:
                    resetterType = typeof(TwoOneTurnResetter);

                    break;
            }
            // BY DEFAULT ONLY ONE TYPE
            //resetterType = typeof(TwoOneTurnResetter);

            //Debug.Log("Algorithm: " + algoCode);
            //switch (pathCode)
            //{
            //    case 1:
            //        pathSeedChoice = PathSeedChoice.Office;
            //        break;
            //    case 2:
            //        pathSeedChoice = PathSeedChoice.ExplorationSmall;
            //        break;
            //    case 3:
            //        pathSeedChoice = PathSeedChoice.ExplorationLarge;
            //        break;
            //    case 4:
            //        pathSeedChoice = PathSeedChoice.LongWalk;
            //        break;
            //    case 5:
            //        pathSeedChoice = PathSeedChoice.ZigZag;
            //        break;
            //}
            //Debug.Log("PathSeed: " + pathSeedChoice);
            switch (condExperiment)
            {
                case ExperimentChoice.FixedTrackedSpace:
                    SetUpExperimentFixedTrackingArea(condPath, redirectorType, resetterType);

                    break;
                case ExperimentChoice.VaryingSizes:
                    SetUpExperimentTrackingAreaSizePerformance(condPath, redirectorType, resetterType);

                    break;
                case ExperimentChoice.VaryingShapes:
                    SetUpExperimentTrackingAreaShape(condPath, redirectorType, resetterType);

                    break;
                //case 3:
                //    setUpExperimentGainFactors(pathSeedChoice, redirectors, resetters);
                //    break;
            }
        }

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);


        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);


        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);


        GenerateAllExperimentSetups();

        // Determine Initial Configurations If Random
        DetermineInitialConfigurations(ref _experimentSetups);
    }


    // Use this for initialization
    private void Start()
    {
    }


    // Update is called once per frame
    private void Update()
    {
        if (redirectionManager.Controller == RedirectionManager.MovementController.Tracker)
        {
            return;
        }
        //framesGoneBy++;
        //if (firstUpdateRealTime == 0)
        //    firstUpdateRealTime = Time.realtimeSinceStartup;
        //if (Time.realtimeSinceStartup - firstUpdateRealTime > 1)
        //{
        //    Debug.Log("Frames Per Second: " + (framesGoneBy / 1.0f));
        //    firstUpdateRealTime = 0;
        //    framesGoneBy = 0;
        //}

        UpdateSimulatedWaypointIfRequired();

        // First Take Care of Snapshot, so the time it take to generate it doesn't effect newly beginning experiment
        if (_takeScreenshot)
        {
            //Debug.Log("Frames In Experiment: " + framesInExperiment);
            _framesInExperiment = 0;
            var start = Time.realtimeSinceStartup;
            redirectionManager.snapshotGenerator.TakeScreenshot(ExperimentDescriptorToString(GetExperimentDescriptor(_experimentSetups[_experimentIterator - 1]))); // Snapshot pertains to the previous experiment
            Debug.Log("Time Spent For Snapshot Generation: " + (Time.realtimeSinceStartup - start));
            _takeScreenshot = false;

            if (_experimentIterator == _experimentSetups.Count)
            {
                Debug.Log("---------- EXPERIMENTS COMPLETE ----------");
            }
        }

        //if (!experimentInProgress && ((Time.time - lastExperimentEndTime) / timeScale > EXPERIMENT_WAIT_TIME) && experimentIterator < experimentSetups.Count)
        if (!experimentInProgress && _experimentIterator < _experimentSetups.Count)
        {
            StartNextExperiment();
            //experimentStartTime = Time.time;
        }

        //if (experimentInProgress && !userStartedWalking && ((Time.time - experimentStartTime) / timeScale > WALKING_WAIT_TIME))
        if (experimentInProgress && !userIsWalking)
        {
            userIsWalking = true;
            //// Allow Walking
            //UserController.allowWalking = true;
            // Start Logging
            redirectionManager.statisticsLogger.BeginLogging();
        }

        if (experimentInProgress && userIsWalking)
        {
            //Debug.Log("User At: " + redirectionManager.userHeadTransform.position.ToString("f4"));
            _framesInExperiment++;
        }
    }


    private void OnGUI()
    {
        //GUI.Box(new Rect((int)(0.5f * Screen.width) - 75, (int)(0.5f * Screen.height) - 14, 150, 28), (1 / (60 * Time.deltaTime)).ToString("f1"));
        if (_experimentComplete)
        {
            GUI.Box(new Rect((int) (0.5f * Screen.width) - 75, (int) (0.5f * Screen.height) - 14, 150, 28), "Experiment Complete");
        }
    }


    private Dictionary<string, string> GetExperimentDescriptor(ExperimentSetup setup)
    {
        var descriptor = new Dictionary<string, string>();

        descriptor["redirector"] = setup.Redirector.ToString();
        descriptor["resetter"] = setup.Resetter == null ? "no_reset" : setup.Resetter.ToString();
        descriptor["tracking_size_x"] = setup.TrackingSizeShape.X.ToString();
        descriptor["tracking_size_z"] = setup.TrackingSizeShape.Z.ToString();

        // OLDER VERBOSE MODE
        //descriptor["redirector"] = setup.redirector.ToString();
        //descriptor["resetter"] = setup.resetter == null ? "no_reset" : setup.resetter.ToString();
        //descriptor["path_waypoint_count"] = setup.pathSeed.waypointCount.ToString();
        //if (setup.pathSeed.distanceDistribution.distributionType == VirtualPathGenerator.DistributionType.Uniform)
        //    descriptor["path_distance_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(min = " + setup.pathSeed.distanceDistribution.min + ", max = " + setup.pathSeed.distanceDistribution.max + ")";
        //if (setup.pathSeed.distanceDistribution.distributionType == VirtualPathGenerator.DistributionType.Normal)
        //    descriptor["path_distance_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(mu = " + setup.pathSeed.distanceDistribution.mu + ", sigma = " + setup.pathSeed.distanceDistribution.sigma + ", min = " + setup.pathSeed.distanceDistribution.min + ", max = " + setup.pathSeed.distanceDistribution.max + ")";
        //if (setup.pathSeed.angleDistribution.distributionType == VirtualPathGenerator.DistributionType.Uniform)
        //    descriptor["path_angle_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(min = " + setup.pathSeed.angleDistribution.min + ", max = " + setup.pathSeed.angleDistribution.max + ")";
        //if (setup.pathSeed.angleDistribution.distributionType == VirtualPathGenerator.DistributionType.Normal)
        //    descriptor["path_angle_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(mu = " + setup.pathSeed.angleDistribution.mu + ", sigma = " + setup.pathSeed.angleDistribution.sigma + ", min = " + setup.pathSeed.angleDistribution.min + ", max = " + setup.pathSeed.angleDistribution.max + ")";
        //descriptor["tracking_size_x"] = setup.trackingSizeShape.x.ToString();
        //descriptor["tracking_size_z"] = setup.trackingSizeShape.z.ToString();
        //descriptor["initial_position"] = setup.initialConfiguration.initialPosition.ToString();
        //descriptor["initial_forward"] = setup.initialConfiguration.initialForward.ToString();
        //descriptor["random_initial_position"] = setup.initialConfiguration.isRandom.ToString();
        //descriptor["trials"] = trialsForCurrentExperiment.ToString();
        //descriptor["g_t_scale_factor"] = setup.gainScaleFactor.x.ToString();
        //descriptor["g_r_scale_factor"] = setup.gainScaleFactor.y.ToString();
        //descriptor["g_c_scale_factor"] = setup.gainScaleFactor.z.ToString();
        return descriptor;
    }


    private void PrintExperimentDescriptor(Dictionary<string, string> experimentDescriptor)
    {
        foreach (var pair in experimentDescriptor)
        {
            Debug.Log(pair.Key + ": " + pair.Value);
        }
    }


    private string ExperimentDescriptorToString(Dictionary<string, string> experimentDescriptor)
    {
        var retVal = "";
        var i = 0;

        foreach (var pair in experimentDescriptor)
        {
            retVal += pair.Value;

            if (i != experimentDescriptor.Count - 1)
            {
                retVal += "+";
            }

            i++;
        }

        return retVal;
    }


    private void SetUserBodyVisibility(bool isVisible)
    {
        print("SetUserBodyVisibility NOT IMPLEMENTED.");
    }


    private void AdjustCameraSizes()
    {
        //redirectionManager.topViewCamera.orthographicSize = 0.5f * (setup.trackingSizeShape.z + SCREENSHOT_EXTRA_COVERAGE_BUFFER);
        print("AdjustCameraSizes NOT IMPLEMENTED.");
    }


    private void AdjustTrailWidth()
    {
        //redirectionManager.realTrailDrawer.PATH_WIDTH = 0.003f * Mathf.Max(setup.trackingSizeShape.x, setup.trackingSizeShape.z);
        print("AdjustTrailWidth NOT IMPLEMENTED.");
    }


    private void AdjustSnapshotGeneratorDimensions()
    {
        //if (setup.trackingSizeShape.x > setup.trackingSizeShape.z)
        //{
        //    redirectionManager.screenshotGenerator.resWidth = ScreenshotGenerator.maxResWidthOrHeight;
        //    redirectionManager.screenshotGenerator.resHeight = (int)Mathf.Ceil(ScreenshotGenerator.maxResWidthOrHeight * ((setup.trackingSizeShape.z + SCREENSHOT_EXTRA_COVERAGE_BUFFER) / (setup.trackingSizeShape.x + SCREENSHOT_EXTRA_COVERAGE_BUFFER)));
        //}
        //else if (setup.trackingSizeShape.x < setup.trackingSizeShape.z)
        //{
        //    redirectionManager.screenshotGenerator.resHeight = ScreenshotGenerator.maxResWidthOrHeight;
        //    redirectionManager.screenshotGenerator.resWidth = (int)Mathf.Ceil(ScreenshotGenerator.maxResWidthOrHeight * ((setup.trackingSizeShape.x + SCREENSHOT_EXTRA_COVERAGE_BUFFER) / (setup.trackingSizeShape.z + SCREENSHOT_EXTRA_COVERAGE_BUFFER)));
        //}
        //else
        //{
        //    redirectionManager.screenshotGenerator.resHeight = ScreenshotGenerator.maxResWidthOrHeight;
        //    redirectionManager.screenshotGenerator.resWidth = ScreenshotGenerator.maxResWidthOrHeight;
        //}
        print("AdjustSnapshotGeneratorDimensions NOT IMPLEMENTED.");
    }


    public List<Dictionary<string, string>> MergeTrialSummaryStatistics(List<Dictionary<string, string>> experimentResults)
    {
        var mergedResults = new List<Dictionary<string, string>>();
        Dictionary<string, string> mergedResult = null;
        float tempValue = 0;
        var tempVectorValue = Vector2.zero;

        for (var i = 0; i < experimentResults.Count; i++)
        {
            if (i % _trialsForCurrentExperiment == 0)
            {
                mergedResult = new Dictionary<string, string>(experimentResults[i]);
            }
            else
            {
                foreach (var pair in experimentResults[i])
                {
                    if (float.TryParse(pair.Value, out tempValue))
                    {
                        //Debug.Log("Averaged Float Values: " + pair.Value + ", " + mergedResult[pair.Key]);
                        mergedResult[pair.Key] = i % _trialsForCurrentExperiment == _trialsForCurrentExperiment - 1 ? ((float.Parse(mergedResult[pair.Key]) + tempValue) / _trialsForCurrentExperiment).ToString() : (float.Parse(mergedResult[pair.Key]) + tempValue).ToString();
                    }
                    else if (TryParseVector2(pair.Value, out tempVectorValue))
                    {
                        //Debug.Log("Averaged Vector Values: " + pair.Value + ", " + mergedResult[pair.Key]);
                        mergedResult[pair.Key] = i % _trialsForCurrentExperiment == _trialsForCurrentExperiment - 1 ? ((ParseVector2(mergedResult[pair.Key]) + tempVectorValue) / _trialsForCurrentExperiment).ToString() : (ParseVector2(mergedResult[pair.Key]) + tempVectorValue).ToString();
                    }
                }
            }

            if (i % _trialsForCurrentExperiment == _trialsForCurrentExperiment - 1)
            {
                mergedResults.Add(mergedResult);
            }
        }

        return mergedResults;
    }


    private bool TryParseVector2(string value, out Vector2 result)
    {
        result = Vector2.zero;

        if (!(value[0] == '(' && value[value.Length - 1] == ')' && value.Contains(",")))
        {
            return false;
        }

        result.x = float.Parse(value.Substring(1, value.IndexOf(",") - 1));
        result.y = float.Parse(value.Substring(value.IndexOf(",") + 2, value.IndexOf(")") - (value.IndexOf(",") + 2)));

        return true;
    }


    private Vector2 ParseVector2(string value)
    {
        var result = Vector2.zero;
        result.x = float.Parse(value.Substring(1, value.IndexOf(",") - 1));
        result.y = float.Parse(value.Substring(value.IndexOf(",") + 2, value.IndexOf(")") - (value.IndexOf(",") + 2)));

        return result;
    }


    private void DetermineInitialConfigurations(ref List<ExperimentSetup> experimentSetups)
    {
        for (var i = 0; i < experimentSetups.Count; i++)
        {
            var setup = experimentSetups[i];

            if (setup.InitialConfiguration.IsRandom)
            {
                if (!onlyRandomizeForward)
                {
                    setup.InitialConfiguration.InitialPosition = VirtualPathGenerator.GetRandomPositionWithinBounds(-0.5f * setup.TrackingSizeShape.X, 0.5f * setup.TrackingSizeShape.X, -0.5f * setup.TrackingSizeShape.Z, 0.5f * setup.TrackingSizeShape.Z);
                }

                setup.InitialConfiguration.InitialForward = VirtualPathGenerator.GetRandomForward();
                //Debug.LogWarning("Random Initial Configuration for size (" + trackingSizeShape.x + ", " + trackingSizeShape.z + "): Pos" + initialConfiguration.initialPosition.ToString("f2") + " Forward" + initialConfiguration.initialForward.ToString("f2"));
                experimentSetups[i] = setup;
            }
            else if (Mathf.Abs(setup.InitialConfiguration.InitialPosition.x) > 0.5f * setup.TrackingSizeShape.X || Mathf.Abs(setup.InitialConfiguration.InitialPosition.y) > 0.5f * setup.TrackingSizeShape.Z)
            {
                Debug.LogError("Invalid beginning position selected. Defaulting Initial Configuration to (0, 0) and (0, 1).");
                setup.InitialConfiguration.InitialPosition = Vector2.zero;
                setup.InitialConfiguration.InitialForward = Vector2.up;
                experimentSetups[i] = setup;
            }

            if (!setup.InitialConfiguration.IsRandom)
            {
                // Deal with diagonal hack
                if (setup.InitialConfiguration.InitialForward == Vector2.one)
                {
                    setup.InitialConfiguration.InitialForward = new Vector2(setup.TrackingSizeShape.X, setup.TrackingSizeShape.Z).normalized;
                    experimentSetups[i] = setup;
                }
            }
        }
    }


    private void UpdateSimulatedWaypointIfRequired()
    {
        if ((redirectionManager.currPos - Utilities.FlattenedPos3D(redirectionManager.targetWaypoint.position)).magnitude < DISTANCE_TO_WAYPOINT_THRESHOLD)
        {
            redirectionManager.simulationManager.UpdateWaypoint();
        }
    }


    public void UpdateWaypoint()
    {
        if (!experimentInProgress)
        {
            return;
        }

        if (waypointIterator == waypoints.Count - 1)
        {
            if (_experimentIterator < _experimentSetups.Count)
            {
                EndExperiment();
            }
        }
        else
        {
            waypointIterator++;
            redirectionManager.targetWaypoint.position = new Vector3(waypoints[waypointIterator].x, redirectionManager.targetWaypoint.position.y, waypoints[waypointIterator].y);
        }
    }


    //enum AlgorithmChoice { S2C, S2O, GreedyTransGain, S2C_GreedyTransGain, S2O_GreedyTransGain, CenterBased, CenterBasedTransGainSpeedUp, S2C_CenterBasedTransGainSpeedUp, S2O_CenterBasedTransGainSpeedUp, None };
    private enum ExperimentChoice
    {
        FixedTrackedSpace,
        VaryingSizes,
        VaryingShapes
    }


    private enum AlgorithmChoice
    {
        None,
        S2C,
        S2O,
        Zigzag
    }


    private enum PathSeedChoice
    {
        Office,
        ExplorationSmall,
        ExplorationLarge,
        LongWalk,
        ZigZag
    }


    private enum ResetChoice
    {
        None,
        TwoOneTurn
    }


    public struct InitialConfiguration
    {
        public Vector2 InitialPosition;
        public Vector2 InitialForward;
        public bool IsRandom;


        public InitialConfiguration(Vector2 initialPosition, Vector2 initialForward)
        {
            InitialPosition = initialPosition;
            InitialForward = initialForward;
            IsRandom = false;
        }


        public InitialConfiguration(bool isRandom) // For Creating Random Configuration or just default of center/up
        {
            InitialPosition = Vector2.zero;
            InitialForward = Vector2.up;
            IsRandom = isRandom;
        }
    }


    private struct TrackingSizeShape
    {
        public readonly float X;
        public readonly float Z;


        public TrackingSizeShape(float x, float z)
        {
            X = x;
            Z = z;
        }
    }


    private struct ExperimentSetup
    {
        public readonly Type Redirector;
        public readonly Type Resetter;
        public readonly VirtualPathGenerator.PathSeed PathSeed;
        public readonly TrackingSizeShape TrackingSizeShape;
        public InitialConfiguration InitialConfiguration;
        public Vector3 GainScaleFactor;


        public ExperimentSetup(Type redirector, Type resetter, VirtualPathGenerator.PathSeed pathSeed, TrackingSizeShape trackingSizeShape, InitialConfiguration initialConfiguration, Vector3 gainScaleFactor)
        {
            Redirector = redirector;
            Resetter = resetter;
            PathSeed = pathSeed;
            TrackingSizeShape = trackingSizeShape;
            InitialConfiguration = initialConfiguration;
            GainScaleFactor = gainScaleFactor;
        }
    }
}