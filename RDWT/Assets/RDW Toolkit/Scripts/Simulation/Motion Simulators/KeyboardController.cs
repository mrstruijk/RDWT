﻿using Redirection;
using UnityEngine;


public class KeyboardController : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;

    /// <summary>
    ///     Auto-Adjust automatically counters curvature as human naturally would.
    /// </summary>
    [SerializeField] private bool useAutoAdjust = true;

    /// <summary>
    ///     Translation speed in meters per second.
    /// </summary>
    [SerializeField] [Range(0.01f, 10)] private float translationSpeed = 1f;

    /// <summary>
    ///     Rotation speed in degrees per second.
    /// </summary>
    [SerializeField] [Range(0.01f, 360)] private float rotationSpeed = 90f;

    private float _lastCurvatureApplied = 0;
    //float lastRotationApplied = 0;
    //Vector3 lastTranslationApplied = Vector3.zero;


    // Use this for initialization
    private void Start()
    {
    }


    // Update is called once per frame
    private void Update()
    {
        if (!redirectionManager.simulationManager.userIsWalking || redirectionManager.Controller != RedirectionManager.MovementController.Keyboard)
        {
            return;
        }

        var userForward = Utilities.FlattenedDir3D(transform.forward);
        var userRight = Utilities.FlattenedDir3D(transform.right);

        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(translationSpeed * Time.deltaTime * userForward, Space.World);
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(-translationSpeed * Time.deltaTime * userForward, Space.World);
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(translationSpeed * Time.deltaTime * userRight, Space.World);
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(-translationSpeed * Time.deltaTime * userRight, Space.World);
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Rotate(userRight, -rotationSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Rotate(userRight, rotationSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
        }


        if (useAutoAdjust)
        {
            transform.Rotate(Vector3.up, -_lastCurvatureApplied, Space.World);
            _lastCurvatureApplied = 0; // We set it to zero meaning we applied what was last placed. This prevents constant application of rotation when curvature isn't applied.

            //this.transform.Rotate(Vector3.up, -lastRotationApplied, Space.World);
            //lastRotationApplied = 0; // We set it to zero meaning we applied what was last placed. This prevents constant application of rotation when rotation isn't applied.

            //this.transform.Translate(-lastTranslationApplied, Space.World);
            //lastTranslationApplied = Vector3.zero; // We set it to zero meaning we applied what was last placed. This prevents constant application of translation when translation isn't applied.
        }
    }


    public void SetLastCurvature(float rotationInDegrees)
    {
        _lastCurvatureApplied = rotationInDegrees;
        //if (useAutoAdjust)
        //{
        //    this.transform.Rotate(Vector3.up, -rotationInDegrees, Space.World);
        //}
    }


    public void SetLastRotation(float rotationInDegrees)
    {
        //lastRotationApplied = rotationInDegrees;
        //if (useAutoAdjust)
        //{
        //    this.transform.Rotate(Vector3.up, -rotationInDegrees, Space.World);
        //}
    }


    public void SetLastTranslation(Vector3 translation)
    {
        //lastTranslationApplied = translation;
        //if (useAutoAdjust)
        //{
        //    this.transform.Translate(-translation, Space.World);
        //}
    }
}