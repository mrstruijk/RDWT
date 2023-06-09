using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Redirection;
using UnityEngine;


public class StatisticsLogger : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;

    public bool logSampleVariables = false;
    public bool appendToFile = false;
    public float samplingFrequency = 10; // How often we will gather data we have and log it in hertz
    public string SUMMARY_STATISTICS_XML_FILENAME = "SimulationResults";

    private StreamWriter _csvWriter;
    private List<float> _curvatureGainSamples = new();
    private List<float> _curvatureGainSamplesBuffer = new();
    private List<float> _distanceToCenterSamples = new();
    private List<float> _distanceToCenterSamplesBuffer = new();
    private List<float> _distanceToNearestBoundarySamples = new();
    private List<float> _distanceToNearestBoundarySamplesBuffer = new();
    private float _experimentBeginningTime = 0;
    private float _experimentEndingTime = 0;
    private List<float> _injectedRotationFromCurvatureGainSamples = new();
    private List<float> _injectedRotationFromCurvatureGainSamplesBuffer = new();
    
    // NOTE: IN THE FUTURE, WE MIGHT WANT TO LOG THE INJECTED VALUES DIVIDED BY TIME, SO IT'S MORE CONSISTENT AND NO DEPENDENT ON THE FRAMERATE
    private List<float> _injectedRotationFromRotationGainSamples = new();
    private List<float> _injectedRotationFromRotationGainSamplesBuffer = new();
    private List<float> _injectedRotationSamples = new();
    private List<float> _injectedRotationSamplesBuffer = new();
    private List<float> _injectedTranslationSamples = new();
    private List<float> _injectedTranslationSamplesBuffer = new();

    private float _lastSamplingTime = 0;
    private float _maxCurvatureGain = float.MinValue;
    private float _maxRotationGain = float.MinValue;
    private float _maxTranslationGain = float.MinValue;
    private float _minCurvatureGain = float.MaxValue;
    private float _minRotationGain = float.MaxValue;
    private float _minTranslationGain = float.MaxValue;

    // Reset Single Parameters
    private float _resetCount = 0;

    ////////////// LOGGING TO FILE

    private string _resultDirectory = "Experiment Results/";
    private List<float> _rotationGainSamples = new();
    private List<float> _rotationGainSamplesBuffer = new();
    private string _sampledMetricsDirectory = "Sampled Metrics/";
    private List<float> _samplingIntervals = new();

    private LoggingState _state = LoggingState.NotStarted;
    private string _summaryStatisticsDirectory = "Summary Statistics/";
    private float _sumOfInjectedRotationFromCurvatureGain = 0; // Overall amount of rotation (IN RADIANS) (around user) of redirection reference due to curvature gain (always positive)
    private float _sumOfInjectedRotationFromRotationGain = 0; // Overall amount of rotation (IN RADIANS) (around user) of redirection reference due to rotation gain (always positive)
    // The way this works is that we wait 1 / samplingFrequency time to transpire before we attempt to clean buffers and gather samples
    // And since we always get a buffer value right before collecting samples, we'll have at least 1 buffer value to get an average from
    // The only problem with this is that overall we'll be gathering less than the expected frequency since the "lateness" of sampling will accumulate

    // THE FOLLOWING PARAMETERS MUST BE SENSITIVE TO TIME SCALE

    // TEMPORARILY SETTING ALL TO PUBLIC FOR TESTING

    // Redirection Single Parameters
    private float _sumOfInjectedTranslation = 0; // Overall amount of displacement (IN METERS) of redirection reference due to translation gain (always positive)
    private float _sumOfRealDistanceTravelled = 0; // Based on user movement controller
    private float _sumOfVirtualDistanceTravelled = 0; // Based on user movement controller plus redirection movement
    private List<float> _timeElapsedBetweenResets = new(); // this will be measured also from beginning to first reset, and from last reset to end (?)
    private float _timeOfLastReset = 0;
    private List<float> _translationGainSamples = new();
    private List<float> _translationGainSamplesBuffer = new();

    // Sampling Paramers: These parameters are first read per frame/value update and stored in their buffer, and then 1/samplingFrequency time goes by, the values in the buffer will be averaged and logged to the list
    // The buffer variables for gains will be multiplied by time and at sampling time divided by time since last sample to get a proper average (since the functions aren't guaranteed to be called every frame)
    // Actually we can do this for all parameters just for true weighted average!
    private List<Vector2> _userRealPositionSamples = new();
    private List<Vector2> _userRealPositionSamplesBuffer = new();
    private List<Vector2> _userVirtualPositionSamples = new();
    private List<Vector2> _userVirtualPositionSamplesBuffer = new();

    // Reset Sample Parameters
    private List<float> _virtualDistancesTravelledBetweenResets = new(); // this will be measured also from beginning to first reset, and from last reset to end (?)
    private float _virtualDistanceTravelledSinceLastReset;

    private XmlWriter _xmlWriter;

    [HideInInspector]
    public List<Dictionary<string, string>> ExperimentResults = new();
    private const string XMLRoot = "Experiments";
    private const string XMLElement = "Experiment";


    // MAKE SURE TO INITIALIZE ALL VALUES HERE
    // FOR NOW I JUST CARE ABOUT RESETS
    private void InitializeAllValues()
    {
        _sumOfInjectedTranslation = 0;
        _sumOfInjectedRotationFromRotationGain = 0;
        _sumOfInjectedRotationFromCurvatureGain = 0;
        _maxTranslationGain = float.MinValue;
        _minTranslationGain = float.MaxValue;
        _maxRotationGain = float.MinValue;
        _minRotationGain = float.MaxValue;
        _maxCurvatureGain = float.MinValue;
        _minCurvatureGain = float.MaxValue;

        _resetCount = 0;
        _sumOfVirtualDistanceTravelled = 0;
        _sumOfRealDistanceTravelled = 0;
        _experimentBeginningTime = redirectionManager.GetTime();

        _virtualDistancesTravelledBetweenResets = new List<float>();
        _virtualDistanceTravelledSinceLastReset = 0;
        _timeElapsedBetweenResets = new List<float>();
        _timeOfLastReset = redirectionManager.GetTime(); // Technically a reset didn't happen here but we want to remember this time point

        _userRealPositionSamples = new List<Vector2>();
        _userRealPositionSamplesBuffer = new List<Vector2>();
        _userVirtualPositionSamples = new List<Vector2>();
        _userVirtualPositionSamplesBuffer = new List<Vector2>();
        _translationGainSamples = new List<float>();
        _translationGainSamplesBuffer = new List<float>();
        _injectedTranslationSamples = new List<float>();
        _injectedTranslationSamplesBuffer = new List<float>();
        _rotationGainSamples = new List<float>();
        _rotationGainSamplesBuffer = new List<float>();
        _injectedRotationFromRotationGainSamples = new List<float>();
        _injectedRotationFromRotationGainSamplesBuffer = new List<float>();
        _curvatureGainSamples = new List<float>();
        _curvatureGainSamplesBuffer = new List<float>();
        _injectedRotationFromCurvatureGainSamples = new List<float>();
        _injectedRotationFromCurvatureGainSamplesBuffer = new List<float>();
        _injectedRotationSamples = new List<float>();
        _injectedRotationSamplesBuffer = new List<float>();
        _distanceToNearestBoundarySamples = new List<float>();
        _distanceToNearestBoundarySamplesBuffer = new List<float>();
        _distanceToCenterSamples = new List<float>();
        _distanceToCenterSamplesBuffer = new List<float>();
        _samplingIntervals = new List<float>();

        _lastSamplingTime = redirectionManager.GetTime();
    }


    // IMPORTANT! The gathering of values has to be in LateUpdate to make sure the "Time.deltaTime" that's used by the gain sampling functions is the same ones that are considered when dividing by time elapsed 
    // that we do when gatherin the samples from buffers. Otherwise it can be that we get the buffers from a deltaTime, then the same deltaTime is used later to calculate a buffer value for a gain, and then 
    // later on the division won't be fair!
    public void UpdateStats()
    {
        if (_state == LoggingState.Logging)
        {
            // Average and Log Sampled Values If It's Time To
            UpdateFrameBasedValues();

            if (redirectionManager.GetTime() - _lastSamplingTime > 1 / samplingFrequency)
            {
                GenerateSamplesFromBufferValuesAndClearBuffers();
                _samplingIntervals.Add(redirectionManager.GetTime() - _lastSamplingTime);
                _lastSamplingTime = redirectionManager.GetTime();
            }
        }
    }


    public void BeginLogging()
    {
        if (_state == LoggingState.NotStarted || _state == LoggingState.Complete)
        {
            _state = LoggingState.Logging;
            InitializeAllValues();
        }
    }


    // IF YOU PAUSE, YOU HAVE TO BE CAREFUL ABOUT TIME ELAPSED BETWEEN PAUSES!
    public void PauseLogging()
    {
        if (_state == LoggingState.Logging)
        {
            _state = LoggingState.Paused;
        }
    }


    public void ResumeLogging()
    {
        if (_state == LoggingState.Paused)
        {
            _state = LoggingState.Logging;
        }
    }


    // Experiment Descriptors are given and 
    public void EndLogging()
    {
        if (_state == LoggingState.Logging)
        {
            Event_Experiment_Ended();
            _state = LoggingState.Complete;
        }
    }


    // Experiment Descriptors are given and we add the logged data as a full experiment result bundle
    public Dictionary<string, string> GetExperimentResultForSummaryStatistics(Dictionary<string, string> experimentDescriptor)
    {
        var experimentResults = new Dictionary<string, string>(experimentDescriptor);

        experimentResults["reset_count"] = _resetCount.ToString();
        experimentResults["virtual_distance_between_resets_median"] = GetMedian(_virtualDistancesTravelledBetweenResets).ToString();
        experimentResults["time_elapsed_between_resets_median"] = GetMedian(_timeElapsedBetweenResets).ToString();

        experimentResults["sum_injected_translation"] = _sumOfInjectedTranslation.ToString();
        experimentResults["sum_injected_rotation_g_r"] = _sumOfInjectedRotationFromRotationGain.ToString();
        experimentResults["sum_injected_rotation_g_c"] = _sumOfInjectedRotationFromCurvatureGain.ToString();
        experimentResults["sum_real_distance_travelled"] = _sumOfRealDistanceTravelled.ToString();
        experimentResults["sum_virtual_distance_travelled"] = _sumOfVirtualDistanceTravelled.ToString();
        experimentResults["min_g_t"] = _minTranslationGain < float.MaxValue ? _minTranslationGain.ToString() : "N/A";
        experimentResults["max_g_t"] = _maxTranslationGain > float.MinValue ? _maxTranslationGain.ToString() : "N/A";
        experimentResults["min_g_r"] = _minRotationGain < float.MaxValue ? _minRotationGain.ToString() : "N/A";
        experimentResults["max_g_r"] = _maxRotationGain > float.MinValue ? _maxRotationGain.ToString() : "N/A";
        experimentResults["min_g_c"] = _minCurvatureGain < float.MaxValue ? _minCurvatureGain.ToString() : "N/A";
        experimentResults["max_g_c"] = _maxCurvatureGain > float.MinValue ? _maxCurvatureGain.ToString() : "N/A";
        experimentResults["g_t_average"] = GetAverageOfAbsoluteValues(_translationGainSamples).ToString();
        experimentResults["injected_translation_average"] = GetAverage(_injectedTranslationSamples).ToString();
        experimentResults["g_r_average"] = GetAverageOfAbsoluteValues(_rotationGainSamples).ToString();
        experimentResults["injected_rotation_from_rotation_gain_average"] = GetAverage(_injectedRotationFromRotationGainSamples).ToString();
        experimentResults["g_c_average"] = GetAverageOfAbsoluteValues(_curvatureGainSamples).ToString();
        experimentResults["injected_rotation_from_curvature_gain_average"] = GetAverage(_injectedRotationFromCurvatureGainSamples).ToString();
        experimentResults["injected_rotation_average"] = GetAverage(_injectedRotationSamples).ToString();

        experimentResults["real_position_average"] = GetAverage(_userRealPositionSamples).ToString();
        experimentResults["virtual_position_average"] = GetAverage(_userVirtualPositionSamples).ToString();
        experimentResults["distance_to_boundary_average"] = GetAverage(_distanceToNearestBoundarySamples).ToString();
        experimentResults["distance_to_center_average"] = GetAverage(_distanceToCenterSamples).ToString();
        experimentResults["normalized_distance_to_boundary_average"] = GetTrackingAreaNormalizedValue(GetAverage(_distanceToNearestBoundarySamples)).ToString();
        experimentResults["normalized_distance_to_center_average"] = GetTrackingAreaNormalizedValue(GetAverage(_distanceToCenterSamples)).ToString();

        experimentResults["experiment_duration"] = (_experimentEndingTime - _experimentBeginningTime).ToString();
        experimentResults["average_sampling_interval"] = GetAverage(_samplingIntervals).ToString();

        return experimentResults;
    }


    public void GetExperimentResultsForSampledVariables(out Dictionary<string, List<float>> oneDimensionalSamples, out Dictionary<string, List<Vector2>> twoDimensionalSamples)
    {
        oneDimensionalSamples = new Dictionary<string, List<float>>();
        twoDimensionalSamples = new Dictionary<string, List<Vector2>>();

        oneDimensionalSamples.Add("distances_to_boundary", _distanceToNearestBoundarySamples);
        oneDimensionalSamples.Add("normalized_distances_to_boundary", GetTrackingAreaNormalizedList(_distanceToNearestBoundarySamples));
        oneDimensionalSamples.Add("distances_to_center", _distanceToCenterSamples);
        oneDimensionalSamples.Add("normalized_distances_to_center", GetTrackingAreaNormalizedList(_distanceToCenterSamples));
        oneDimensionalSamples.Add("g_t", _translationGainSamples);
        oneDimensionalSamples.Add("injected_translations", _injectedTranslationSamples);
        oneDimensionalSamples.Add("g_r", _rotationGainSamples);
        oneDimensionalSamples.Add("injected_rotations_from_rotation_gain", _injectedRotationFromRotationGainSamples);
        oneDimensionalSamples.Add("g_c", _curvatureGainSamples);
        oneDimensionalSamples.Add("injected_rotations_from_curvature_gain", _injectedRotationFromCurvatureGainSamples);
        oneDimensionalSamples.Add("injected_rotations", _injectedRotationSamples);
        oneDimensionalSamples.Add("virtual_distances_between_resets", _virtualDistancesTravelledBetweenResets);
        oneDimensionalSamples.Add("time_elapsed_between_resets", _timeElapsedBetweenResets);
        oneDimensionalSamples.Add("sampling_intervals", _samplingIntervals);

        twoDimensionalSamples.Add("user_real_positions", _userRealPositionSamples);
        twoDimensionalSamples.Add("user_virtual_positions", _userVirtualPositionSamples);
    }


    public void Event_User_Translated(Vector3 deltaPosition2D)
    {
        if (_state == LoggingState.Logging)
        {
            _sumOfVirtualDistanceTravelled += deltaPosition2D.magnitude;
            _sumOfRealDistanceTravelled += deltaPosition2D.magnitude;
            _virtualDistanceTravelledSinceLastReset += deltaPosition2D.magnitude;
        }
    }


    public void Event_User_Rotated(float rotationInDegrees)
    {
        if (_state == LoggingState.Logging)
        {
        }
    }


    public void Event_Translation_Gain(float gT, Vector3 translationApplied)
    {
        if (_state == LoggingState.Logging)
        {
            //if (testing)
            //{
            //    g_t = 1;
            //    translationApplied = Vector2.up;
            //}

            _sumOfInjectedTranslation += translationApplied.magnitude;
            _maxTranslationGain = Mathf.Max(_maxTranslationGain, gT);
            _minTranslationGain = Mathf.Min(_minTranslationGain, gT);
            _sumOfVirtualDistanceTravelled += Mathf.Sign(gT) * translationApplied.magnitude; // if gain is positive, redirection reference moves with the user, thus increasing the virtual displacement, and if negative, decreases
            _virtualDistanceTravelledSinceLastReset += Mathf.Sign(gT) * translationApplied.magnitude;
            //translationGainSamplesBuffer.Add(Mathf.Abs(g_t) * redirectionManager.userMovementManager.lastDeltaTime);
            // The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            // But we'll artificially use this current delta time instead!
            //translationGainSamplesBuffer.Add(g_t * redirectionManager.userMovementManager.lastDeltaTime);
            //print("Translation Gain: " + g_t + "\tInterval: " + redirectionManager.getDeltaTime());
            _translationGainSamplesBuffer.Add(gT * redirectionManager.GetDeltaTime());
            //injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.userMovementManager.lastDeltaTime);
            _injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.GetDeltaTime());
        }
    }


    public void Event_Translation_Gain_Reorientation(float gT, Vector3 translationApplied)
    {
        if (_state == LoggingState.Logging)
        {
            throw new NotImplementedException();
            ////if (testing)
            ////{
            ////    g_t = 1;
            ////    translationApplied = Vector2.up;
            ////}

            //sumOfInjectedTranslation += translationApplied.magnitude;
            //maxTranslationGain = Mathf.Max(maxTranslationGain, g_t);
            //minTranslationGain = Mathf.Min(minTranslationGain, g_t);
            //sumOfVirtualDistanceTravelled += Mathf.Sign(g_t) * translationApplied.magnitude; // if gain is positive, redirection reference moves with the user, thus increasing the virtual displacement, and if negative, decreases
            //virtualDistanceTravelledSinceLastReset += Mathf.Sign(g_t) * translationApplied.magnitude;
            ////translationGainSamplesBuffer.Add(Mathf.Abs(g_t) * redirectionManager.userMovementManager.lastDeltaTime);
            //// The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            //// But we'll artificially use this current delta time instead!
            ////translationGainSamplesBuffer.Add(g_t * redirectionManager.userMovementManager.lastDeltaTime);
            ////print("Translation Gain: " + g_t + "\tInterval: " + redirectionManager.getDeltaTime());
            //translationGainSamplesBuffer.Add(g_t * redirectionManager.getDeltaTime());
            ////injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.userMovementManager.lastDeltaTime);
            //injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.getDeltaTime());
        }
    }


    public void Event_Rotation_Gain(float gR, float rotationApplied)
    {
        if (_state == LoggingState.Logging)
        {
            //if (testing)
            //{
            //    g_r = 1;
            //    rotationApplied = 1;
            //}
            _sumOfInjectedRotationFromRotationGain += Mathf.Abs(rotationApplied);
            _maxRotationGain = Mathf.Max(_maxRotationGain, gR);
            _minRotationGain = Mathf.Min(_minRotationGain, gR);
            //rotationGainSamplesBuffer.Add(Mathf.Abs(g_r) * redirectionManager.userMovementManager.lastDeltaTime);
            // The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            // But we'll artificially use this current delta time instead!
            //rotationGainSamplesBuffer.Add(g_r * redirectionManager.userMovementManager.lastDeltaTime);
            _rotationGainSamplesBuffer.Add(gR * redirectionManager.GetDeltaTime());
            //injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            _injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
            //injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            _injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
        }
    }


    public void Event_Rotation_Gain_Reorientation(float gR, float rotationApplied)
    {
        if (_state == LoggingState.Logging)
        {
            print("event_rotation_gain_reorientation NOT IMPLEMENTED.");
            //throw new System.NotImplementedException();

            ////if (testing)
            ////{
            ////    g_r = 1;
            ////    rotationApplied = 1;
            ////}
            //sumOfInjectedRotationFromRotationGain += Mathf.Abs(rotationApplied);
            //maxRotationGain = Mathf.Max(maxRotationGain, g_r);
            //minRotationGain = Mathf.Min(minRotationGain, g_r);
            ////rotationGainSamplesBuffer.Add(Mathf.Abs(g_r) * redirectionManager.userMovementManager.lastDeltaTime);
            //// The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            //// But we'll artificially use this current delta time instead!
            ////rotationGainSamplesBuffer.Add(g_r * redirectionManager.userMovementManager.lastDeltaTime);
            //rotationGainSamplesBuffer.Add(g_r * redirectionManager.getDeltaTime());
            ////injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            //injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.getDeltaTime());
            ////injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            //injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.getDeltaTime());
        }
    }


    public void Event_Curvature_Gain(float gC, float rotationApplied)
    {
        if (_state == LoggingState.Logging)
        {
            //if (testing)
            //{
            //    g_c = 1;
            //    rotationApplied = 1;
            //}
            _sumOfInjectedRotationFromCurvatureGain += Mathf.Abs(rotationApplied);
            _maxCurvatureGain = Mathf.Max(_maxCurvatureGain, gC);
            _minCurvatureGain = Mathf.Min(_minCurvatureGain, gC);
            //curvatureGainSamplesBuffer.Add(Mathf.Abs(g_c) * redirectionManager.userMovementManager.lastDeltaTime);
            //// The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            // But we'll artificially use this current delta time instead!
            //curvatureGainSamplesBuffer.Add(g_c * redirectionManager.userMovementManager.lastDeltaTime);
            _curvatureGainSamplesBuffer.Add(gC * redirectionManager.GetDeltaTime());
            //injectedRotationFromCurvatureGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            _injectedRotationFromCurvatureGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
            //injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            _injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
        }
    }


    public void Event_Reset_Triggered()
    {
        if (_state == LoggingState.Logging)
        {
            _resetCount++;
            _virtualDistancesTravelledBetweenResets.Add(_virtualDistanceTravelledSinceLastReset);
            _virtualDistanceTravelledSinceLastReset = 0;
            _timeElapsedBetweenResets.Add(redirectionManager.GetTime() - _timeOfLastReset);
            _timeOfLastReset = redirectionManager.GetTime(); // Technically a reset didn't happen here but we want to remember this time point
        }
    }


    private void UpdateFrameBasedValues()
    {
        ////if (testing)
        ////{
        ////    userRealPositionSamplesBuffer.Add(redirectionManager.getDeltaTime() * Vector2.one);
        ////    distanceToNearestBoundarySamplesBuffer.Add(redirectionManager.getDeltaTime() * 1);
        ////    distanceToCenterSamplesBuffer.Add(redirectionManager.getDeltaTime() * 1);
        ////}
        //else
        //{

        // Now we are letting the developer determine the movement manually in update, and we pull the info from redirector
        Event_User_Rotated(redirectionManager.deltaDir);
        Event_User_Translated(Utilities.FlattenedPos2D(redirectionManager.deltaPos));

        _userRealPositionSamplesBuffer.Add(redirectionManager.GetDeltaTime() * Utilities.FlattenedPos2D(redirectionManager.currPosReal));
        _userVirtualPositionSamplesBuffer.Add(redirectionManager.GetDeltaTime() * Utilities.FlattenedPos2D(redirectionManager.currPos));
        _distanceToNearestBoundarySamplesBuffer.Add(redirectionManager.GetDeltaTime() * redirectionManager.resetter.GetDistanceToNearestBoundary());
        _distanceToCenterSamplesBuffer.Add(redirectionManager.GetDeltaTime() * redirectionManager.currPosReal.magnitude);
        //}
    }


    private void GenerateSamplesFromBufferValuesAndClearBuffers()
    {
        GetSampleFromBuffer(ref _userRealPositionSamples, ref _userRealPositionSamplesBuffer);
        GetSampleFromBuffer(ref _userVirtualPositionSamples, ref _userVirtualPositionSamplesBuffer);
        GetSampleFromBuffer(ref _translationGainSamples, ref _translationGainSamplesBuffer);
        GetSampleFromBuffer(ref _injectedTranslationSamples, ref _injectedTranslationSamplesBuffer);
        GetSampleFromBuffer(ref _rotationGainSamples, ref _rotationGainSamplesBuffer);
        GetSampleFromBuffer(ref _injectedRotationFromRotationGainSamples, ref _injectedRotationFromRotationGainSamplesBuffer);
        GetSampleFromBuffer(ref _curvatureGainSamples, ref _curvatureGainSamplesBuffer);
        GetSampleFromBuffer(ref _injectedRotationFromCurvatureGainSamples, ref _injectedRotationFromCurvatureGainSamplesBuffer);
        GetSampleFromBuffer(ref _injectedRotationSamples, ref _injectedRotationSamplesBuffer);
        GetSampleFromBuffer(ref _distanceToNearestBoundarySamples, ref _distanceToNearestBoundarySamplesBuffer);
        GetSampleFromBuffer(ref _distanceToCenterSamples, ref _distanceToCenterSamplesBuffer);
    }


    private void GetSampleFromBuffer(ref List<float> samples, ref List<float> buffer, bool verbose = false)
    {
        float sampleValue = 0;

        foreach (var bufferValue in buffer)
        {
            sampleValue += bufferValue;
        }

        //samples.Add(sampleValue / (redirectionManager.GetTime() - lastSamplingTime));
        // OPTIONALLY WE CAN NOT LOG ANYTHING AT ALL IN THIS CASE!
        samples.Add(buffer.Count != 0 ? sampleValue / buffer.Count : 0);

        if (verbose)
        {
            print("sampleValue: " + sampleValue);
            print("samplingInterval: " + (redirectionManager.GetTime() - _lastSamplingTime));
        }

        buffer.Clear();
    }


    private void GetSampleFromBuffer(ref List<Vector2> samples, ref List<Vector2> buffer)
    {
        var sampleValue = Vector2.zero;

        foreach (var bufferValue in buffer)
        {
            sampleValue += bufferValue;
        }

        //samples.Add(sampleValue / (redirectionManager.GetTime() - lastSamplingTime));
        samples.Add(sampleValue / buffer.Count);
        buffer.Clear();
    }


    private void Event_Experiment_Ended()
    {
        _virtualDistancesTravelledBetweenResets.Add(_virtualDistanceTravelledSinceLastReset);
        _timeElapsedBetweenResets.Add(redirectionManager.GetTime() - _timeOfLastReset);
        _experimentEndingTime = redirectionManager.GetTime();
    }


    // This function introduces lots of floating point error and I'd rather see clean values than noisy accurate weighted measurements
    //float getTimeWeightedSampleAverage(List<float> sampleArray, List<float> sampleDurationArray)
    //{
    //    float valueSum = 0;
    //    float timeSum = 0;
    //    for (int i = 0; i < sampleArray.Count; i++)
    //    {
    //        valueSum += sampleArray[i] * sampleDurationArray[i];
    //        timeSum += sampleDurationArray[i];
    //    }
    //    return valueSum / timeSum;
    //}


    private Vector2 GetTimeWeightedSampleAverage(List<Vector2> sampleArray, List<float> sampleDurationArray)
    {
        var valueSum = Vector2.zero;
        float timeSum = 0;

        for (var i = 0; i < sampleArray.Count; i++)
        {
            valueSum += sampleArray[i] * sampleDurationArray[i];
            timeSum += sampleDurationArray[i];
        }

        return sampleArray.Count != 0 ? valueSum / timeSum : Vector2.zero;
    }


    private float GetAverage(List<float> array)
    {
        float sum = 0;

        foreach (var value in array)
        {
            sum += value;
        }

        return array.Count != 0 ? sum / array.Count : 0;
    }


    private float GetAverageOfAbsoluteValues(List<float> array)
    {
        float sum = 0;

        foreach (var value in array)
        {
            sum += Mathf.Abs(value);
        }

        return array.Count != 0 ? sum / array.Count : 0;
    }


    private Vector2 GetAverage(List<Vector2> array)
    {
        var sum = Vector2.zero;

        foreach (var value in array)
        {
            sum += value;
        }

        return sum / array.Count;
    }


    // We're not providing a time-based version of this at this time
    private float GetMedian(List<float> array)
    {
        if (array.Count == 0)
        {
            Debug.LogError("Empty Array");

            return 0;
        }

        var sortedArray = array.OrderBy(item => item).ToList();

        if (sortedArray.Count % 2 == 1)
        {
            return sortedArray[(int) (0.5f * sortedArray.Count)];
        }

        return 0.5f * (sortedArray[(int) (0.5f * sortedArray.Count)] + sortedArray[(int) (0.5f * sortedArray.Count) - 1]);
    }


    // This would make more sense in the context of square-shaped environments
    // Normalizing by dividing by diameter
    private float GetTrackingAreaNormalizedValue(float distance)
    {
        return distance / redirectionManager.resetter.GetTrackingAreaHalfDiameter();
    }


    private List<float> GetTrackingAreaNormalizedList(List<float> distances)
    {
        var retVal = new List<float>(distances);

        for (var i = 0; i < distances.Count; i++)
        {
            retVal[i] = retVal[i] / redirectionManager.resetter.GetTrackingAreaHalfDiameter();
        }

        return retVal;
    }


    private void Awake()
    {
        _resultDirectory = SnapshotGenerator.GetProjectPath() + _resultDirectory;
        _summaryStatisticsDirectory = _resultDirectory + _summaryStatisticsDirectory;
        _sampledMetricsDirectory = _resultDirectory + _sampledMetricsDirectory;
        SnapshotGenerator.DefaultSnapshotDirectory = _resultDirectory + SnapshotGenerator.DefaultSnapshotDirectory;
        SnapshotGenerator.CreateDirectoryIfNeeded(_resultDirectory);
        SnapshotGenerator.CreateDirectoryIfNeeded(_summaryStatisticsDirectory);
        SnapshotGenerator.CreateDirectoryIfNeeded(_sampledMetricsDirectory);
        SnapshotGenerator.CreateDirectoryIfNeeded(SnapshotGenerator.DefaultSnapshotDirectory);
    }


    // Writes all summary statistics for a batch of experiments
    public void LogExperimentSummaryStatisticsResults(List<Dictionary<string, string>> experimentResults)
    {
        // Settings
        var settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.IndentChars = "\t";
        settings.CloseOutput = true;

        // Create XML File
        //xmlWriter = redirectionManager.runInTestMode ? XmlWriter.Create(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + "_" + SimulationManager.commandLineRunCode + ".xml", settings) : XmlWriter.Create(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + "_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".xml", settings);
        //xmlWriter = XmlWriter.Create(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + " - " + redirectionManager.startTimeOfProgram + ".xml", settings);
        _xmlWriter = XmlWriter.Create(_summaryStatisticsDirectory + SUMMARY_STATISTICS_XML_FILENAME + ".xml", settings);
        _xmlWriter.Settings.Indent = true;
        _xmlWriter.WriteStartDocument();
        _xmlWriter.WriteStartElement(XMLRoot);

        // HACK: If there's only one element, Excel won't show the lablels, so we're duplicating for now
        if (experimentResults.Count == 1)
        {
            experimentResults.Add(experimentResults[0]);
        }

        foreach (var experimentResult in experimentResults)
        {
            _xmlWriter.WriteStartElement(XMLElement);

            foreach (var entry in experimentResult)
            {
                _xmlWriter.WriteElementString(entry.Key, entry.Value);
            }

            _xmlWriter.WriteEndElement();
        }

        _xmlWriter.WriteEndElement();
        _xmlWriter.WriteEndDocument();
        _xmlWriter.Flush();
        _xmlWriter.Close();
    }


    public void LogExperimentSummaryStatisticsResultsScsv(List<Dictionary<string, string>> experimentResults)
    {
        _csvWriter = new StreamWriter(_summaryStatisticsDirectory + SUMMARY_STATISTICS_XML_FILENAME + ".csv", appendToFile);
        _csvWriter.WriteLine("sep=;");

        if (experimentResults.Count > 0)
        {
            // Set up the headers
            _csvWriter.Write("experiment_start_time;");

            foreach (var header in experimentResults[0].Keys)
            {
                _csvWriter.Write(header + ";");
            }

            _csvWriter.WriteLine();
            // Write Values
            _csvWriter.Write(redirectionManager.startTimeOfProgram + ";");

            foreach (var experimentResult in experimentResults)
            {
                foreach (var value in experimentResult.Values)
                {
                    _csvWriter.Write(value + ";");
                }

                _csvWriter.WriteLine();
            }
        }

        _csvWriter.Flush();
        _csvWriter.Close();
    }


    public void LogOneDimensionalExperimentSamples(string experimentDecriptorString, string measuredMetric, List<float> values)
    {
        //csvWriter = new StreamWriter(SAMPLED_METRICS_DIRECTORY + measuredMetric +"_" + experimentDecriptorString +".csv");
        var experimentSamplesDirectory = _sampledMetricsDirectory + experimentDecriptorString + "/";
        SnapshotGenerator.CreateDirectoryIfNeeded(experimentSamplesDirectory);
        _csvWriter = new StreamWriter(experimentSamplesDirectory + measuredMetric + ".csv");

        foreach (var value in values)
        {
            _csvWriter.WriteLine(value);
        }

        _csvWriter.Flush();
        _csvWriter.Close();
    }


    public void LogTwoDimensionalExperimentSamples(string experimentDecriptorString, string measuredMetric, List<Vector2> values)
    {
        //csvWriter = new StreamWriter(measuredMetric + "_" + experimentDecriptorString + ".csv");
        var experimentSamplesDirectory = _sampledMetricsDirectory + experimentDecriptorString + "/";
        SnapshotGenerator.CreateDirectoryIfNeeded(experimentSamplesDirectory);
        _csvWriter = new StreamWriter(experimentSamplesDirectory + measuredMetric + ".csv");

        foreach (var value in values)
        {
            _csvWriter.WriteLine(value.x + ", " + value.y);
        }

        _csvWriter.Flush();
        _csvWriter.Close();
    }


    public void LogAllExperimentSamples(string experimentDecriptorString, Dictionary<string, List<float>> oneDimensionalSamplesMap, Dictionary<string, List<Vector2>> twoDimensionalSamplesMap)
    {
        foreach (var oneDimensionalSamples in oneDimensionalSamplesMap)
        {
            LogOneDimensionalExperimentSamples(experimentDecriptorString, oneDimensionalSamples.Key, oneDimensionalSamples.Value);
        }

        foreach (var twoDimensionalSamples in twoDimensionalSamplesMap)
        {
            LogTwoDimensionalExperimentSamples(experimentDecriptorString, twoDimensionalSamples.Key, twoDimensionalSamples.Value);
        }
    }


    public void GenerateBatchFiles()
    {
        StreamWriter batchFileWriter;

        for (var pathCode = 1; pathCode <= 4; pathCode++)
        {
            var experimentSamplesDirectory = "batch" + pathCode + ".bat";
            batchFileWriter = new StreamWriter(experimentSamplesDirectory);

            for (var expCode = 1; expCode <= 3; expCode++)
            {
                for (var algoCode = 1; algoCode <= 6; algoCode++)
                {
                    batchFileWriter.WriteLine("start Simulation.exe -batchmode " + expCode + pathCode + algoCode);
                }
            }

            batchFileWriter.Flush();
            batchFileWriter.Close();
        }
    }


    //private bool testing = true;


    // Helper Varialbles
    //float startTime = float.MaxValue;
    private enum LoggingState
    {
        NotStarted,
        Logging,
        Paused,
        Complete
    }
}