﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal;
using GoogleARCore;
using GoogleARCore.Examples.AugmentedImage;

public class ZScanMarker : MonoBehaviour
{
    public static Matrix4x4 world_in_marker;

    public GameObject MarkerPrefab;

    private Transform transformParent;

    public bool MarkerTrackingUpdate()
    {
#if UNITY_EDITOR
        return true;
#else
        return Global.DeviceType == DeviceTypeEnum.NRLight ? NRLightMarkerTrackingUpdate() : ARCoreMarkerTrackingUpdate();
#endif

    }


    int scanCount = 0;
    List<NRTrackableImage> m_NRLightTrackingImages = new List<NRTrackableImage>();
    private bool NRLightMarkerTrackingUpdate()
    {
        NRFrame.GetTrackables<NRTrackableImage>(m_NRLightTrackingImages, NRTrackableQueryFilter.All);
        foreach (var item in m_NRLightTrackingImages)
        {
            if (item.GetTrackingState() == NRKernal.TrackingState.Tracking)
            {

                MarkerPrefab.SetActive(true);
                NRAnchor anchor = item.CreateAnchor();
                MarkerPrefab.transform.position = anchor.transform.position;
                MarkerPrefab.transform.rotation = anchor.transform.rotation;
                //MarkerPrefab.transform.localScale = new Vector3(item.Size.x, MarkerPrefab.transform.localScale.y, item.Size.y);

                if (NRInput.GetButtonDown(ControllerButton.TRIGGER))
                {
                    var marker_in_world = ZUtils.GetTMatrix(anchor.transform.position, anchor.transform.rotation);//ZUtils.GetTMatrix(item.GetCenterPose().position, item.GetCenterPose().rotation);
                    world_in_marker = Matrix4x4.Inverse(marker_in_world);

                    GameObject nrCam = GameObject.Find("NRCameraRig");
                    //GameObject nrInput = GameObject.Find("NRInput");

                    TranslatePose(nrCam.transform);

                    SwitchImageTrackingMode(false); // 关闭maker识别

                    return true;
                }
                scanCount++;
            }
            else
            {
                MarkerPrefab.SetActive(false);
            }
        }
        return false;

    }

    public void SwitchImageTrackingMode(bool on)
    {
        var config = NRSessionManager.Instance.NRSessionBehaviour.SessionConfig;
        config.ImageTrackingMode = on ? TrackableImageFindingMode.ENABLE : TrackableImageFindingMode.DISABLE;
        NRSessionManager.Instance.SetConfiguration(config);
    }



    private List<AugmentedImage> m_TempAugmentedImages = new List<AugmentedImage>();
    private bool ARCoreMarkerTrackingUpdate()
    {
        if (Session.Status != SessionStatus.Tracking)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        Session.GetTrackables<AugmentedImage>(m_TempAugmentedImages, TrackableQueryFilter.Updated);

        foreach (var item in m_TempAugmentedImages)
        {
            if (item.TrackingState == GoogleARCore.TrackingState.Tracking)
            {
                Anchor anchor = item.CreateAnchor(item.CenterPose);
                MarkerPrefab.SetActive(true);
                MarkerPrefab.transform.position = anchor.transform.position;
                MarkerPrefab.transform.rotation = anchor.transform.rotation;
                MarkerPrefab.transform.localScale = new Vector3(0.4f, MarkerPrefab.transform.localScale.y, 0.4f);

                if (Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began)
                {
                    var marker_in_world = ZUtils.GetTMatrix(item.CenterPose.position, item.CenterPose.rotation);
                    world_in_marker = Matrix4x4.Inverse(marker_in_world);

                    GameObject arcoreCam = GameObject.Find("ARCore Device");

                    TranslatePose(arcoreCam.transform);

                    MarkerPrefab.SetActive(false);

                    return true;
                }
                scanCount++;
            }
            else if (item.TrackingState == GoogleARCore.TrackingState.Stopped)
            {
                MarkerPrefab.SetActive(false);
            }
        }

        return false;
    }

    private void TranslatePose(Transform camera)
    {
        if (transformParent == null)
        {
            transformParent = new GameObject("---TransformParent---").transform;
        }

        transformParent.position = camera.position;
        transformParent.rotation = camera.rotation;

        camera.SetParent(transformParent);

        transformParent.position = ZUtils.GetPositionFromTMatrix(world_in_marker);
        transformParent.rotation = ZUtils.GetRotationFromTMatrix(world_in_marker);

        camera.SetParent(null);
    }


}
