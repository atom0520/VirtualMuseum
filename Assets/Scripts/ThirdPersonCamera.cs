using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour {

    Transform cameraTransform;
    [SerializeField]
    float distance = 7.0f;
    [SerializeField]
    float xSpeed = 100;
    [SerializeField]
    float ySpeed = 100;
    [SerializeField]
    float mSpeed = 10;
    [SerializeField]
    float angularSmoothLag = 0.3f;
    [SerializeField]
    float angularMaxSpeed = 15.0f;
    [SerializeField]
    float snapSmoothLag = 0.2f;
    [SerializeField]
    float snapMaxSpeed = 720.0f;
    [SerializeField]
    float clampHeadPositionScreenSpace = 0.75f;


    Transform _target;

    Camera mainCamera;

    ThirdPersonController controller;

    Vector3 headOffset = Vector3.zero;
    Vector3 centerOffset = Vector3.zero;


    bool dosnap = false;
    bool snapped = false;
    bool firstPersonLook = false;
    float angleVelocity = 0f;

    float minAngleY = -45;
    float yTopLimit = -20.0f;
    float yMinLimit = -45;
    float yMaxLimit = 45;
    float minDistance = 1.2f;
    float maxDistance = 3.5f;


    float current_ver_angle = 0.0f;
    float current_hor_angle = 0.0f;
    float look_height = 0.0f;

    bool bSeePicture = false;
    Vector3 curPicturePos;
    Quaternion curPictureRotation;
    Transform curPictureTran;

    bool needRefreshCameraPos;
    Quaternion oldCameraRotation;

    // Use this for initialization
    void Awake () {
        mainCamera = Camera.main;
        cameraTransform = GameObject.Find("Main Camera").transform;

        if (!cameraTransform && mainCamera)
        {
            cameraTransform = mainCamera.transform;
        }

        if (!cameraTransform)
        {
            Debug.Log("Please assign a camera to the ThirdPersonCamera script.");
            enabled = false;
        }

        _target = transform;
        if (_target)
        {
            controller = _target.GetComponent<ThirdPersonController>();
        }

        if (controller)
        {
            CharacterController characterController = (CharacterController)_target.GetComponent<Collider>();
            centerOffset = characterController.bounds.center - _target.position;
            headOffset = centerOffset;

            Transform look_target = _target.Find("LookTarget");

            Vector3 head_back_pos = characterController.bounds.max;
            if (look_target)
            {
                head_back_pos = look_target.transform.position;
            }

            RaycastHit hit_test;
            Vector3 head_top = characterController.bounds.center;
            head_top.y = characterController.bounds.min.y;

            if (Physics.Raycast(head_top, Vector3.down, out hit_test, 50))
            {
                look_height = head_back_pos.y - hit_test.point.y;
            }

            headOffset.y = head_back_pos.y - _target.position.y;

            /*下面计算、保存 相机稳定后 的初始位置与方位*/
            float hor_angle = _target.eulerAngles.y;
            Quaternion rotation_h = Quaternion.Euler(0, hor_angle, 0);
            Vector3 camera_pos = head_back_pos;

            camera_pos += rotation_h * Vector3.back * distance; /*计算相机位置是用 头部为球中心计算的*/

            Vector3 offsetToCenter = head_back_pos - camera_pos;
            Quaternion rotation = Quaternion.LookRotation(new Vector3(offsetToCenter.x, offsetToCenter.y, offsetToCenter.z));
            current_hor_angle = 360 - rotation.eulerAngles.y;
            current_ver_angle = rotation.eulerAngles.x;
        }
        else
        {
            Debug.Log("Please assign a target to the camera that has a ThirdPersonController script attached.");
        }

        Cut(_target, centerOffset);
    }

    void SetVisible(bool visible)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
        if (visible)
        {
            foreach (Renderer rend in renderers)
            {
                rend.enabled = true;
            }
            firstPersonLook = false;
        }
        else
        {
            foreach (Renderer rend in renderers)
            {
                rend.enabled = false;
            }
            firstPersonLook = true;
        }
    }

    void Cut(Transform dummyTarget, Vector3 dummyCenter)
    {
        var oldSnapMaxSpeed = snapMaxSpeed;
        var oldSnapSmooth = snapSmoothLag;

        snapMaxSpeed = 10000;
        snapSmoothLag = 0.001f;

        dosnap = true;

        Apply(transform, Vector3.zero);

        snapMaxSpeed = oldSnapMaxSpeed;
        snapSmoothLag = oldSnapSmooth;
    }

    void DebugDrawStuff()
    {
        Debug.DrawLine(_target.position, _target.position + headOffset);
    }

    float AngleDistance(float a, float b)
    {
        a = Mathf.Repeat(a, 360);
        b = Mathf.Repeat(b, 360);

        return Mathf.Abs(b - a);
    }

    void Apply(Transform dummyTarget, Vector3 dummyCenter)
    {

        // Early out if we don't have a target
        if (!controller)
        {
            return;
        }

        bool needGoOn = false;
        Vector3 targetCenter = _target.position + centerOffset;
        Vector3 targetHead = _target.position + headOffset;

        var strength = Input.GetAxis("Mouse ScrollWheel");
        if (strength != 0)
        {
            distance -= strength * mSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            /*
            if(distance <= 1)
            {
                SetVisible(false);
                minAngleY = -80;		
            }	
            else if(firstPersonLook)
            {
                SetVisible(true);
            }	
            else if(distance < look_height)
            {
                minAngleY = (distance - 2) * (yTopLimit - yMinLimit)/(look_height - 2) - yTopLimit;	
                minAngleY = - minAngleY;		
            }
            else
            {
                minAngleY = yMinLimit;
            }
            */
            needGoOn = true;
        }

        var originalTargetAngle = 360 - _target.eulerAngles.y;
        current_hor_angle = 360 - cameraTransform.eulerAngles.y;
        if (!snapped)
        {
            var targetAngle = originalTargetAngle;
            float dis_angle = 0;
            if (dosnap)
            {
                dis_angle = AngleDistance(360 - current_hor_angle, originalTargetAngle);
                current_hor_angle = Mathf.SmoothDampAngle(current_hor_angle, targetAngle, ref angleVelocity, snapSmoothLag, snapMaxSpeed);
            }

            // We are close to the target, so we can stop snapping now!
            dis_angle = 0;
            if (dis_angle <= 10)
            {
                snapped = true;
                dosnap = false;

            }
            else if (dis_angle < 3)
            {
                dosnap = false;
            }
            if (!snapped && !dosnap)
            {
                current_hor_angle = Mathf.SmoothDampAngle(current_hor_angle, targetAngle, ref angleVelocity, angularSmoothLag, angularMaxSpeed);
            }
            needGoOn = true;
        }
        else
        {
            float rotation_h = 0;
            float rotation_v = 0;
            if (Input.GetMouseButton(1))
            {
                rotation_h = -Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                rotation_v = -Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            }
            needGoOn = needGoOn || (rotation_h != 0 || rotation_v != 0);

            current_hor_angle += rotation_h;
            current_hor_angle = Mathf.Repeat(current_hor_angle, 360);
            current_ver_angle += rotation_v;
            current_ver_angle = Mathf.Clamp(current_ver_angle, minAngleY, yMaxLimit);

        }

        needGoOn = needGoOn || controller.IsMoving();
        needGoOn = needGoOn || controller.IsJumping();

        needGoOn = needGoOn || needRefreshCameraPos;
        needRefreshCameraPos = false;

        //if (!needGoOn)/*没有鼠标键盘事件，返回即可，相机一般不会自动更新。除非未来有其他情形，那时候再添加*/
        //{
        //    MouseMoveContr mousecl = GetComponent<MouseMoveContr>();
        //    bool mouseMoveFlag = mousecl.GetMouseMoveFlag();
            
        //    if (!mouseMoveFlag)
        //    {
        //        return;
        //    }
        //}

  
        float rad_angle_h = (current_hor_angle - 90.0f) * Mathf.Deg2Rad;
        var rad_angle_v = current_ver_angle * Mathf.Deg2Rad;
        var camera_pos = Vector3.zero;
        var radius_hor = distance * Mathf.Cos(rad_angle_v);
        float slope = -Mathf.Sin(rad_angle_v);

        camera_pos.x = radius_hor * Mathf.Cos(rad_angle_h) + targetHead.x;/*计算相机位置是用 头部为球中心计算的*/
        camera_pos.z = radius_hor * Mathf.Sin(rad_angle_h) + targetHead.z;
        camera_pos.y = -distance * slope + targetHead.y;
        if (camera_pos.y < targetHead.y - look_height)
        {
            camera_pos.y = targetHead.y - look_height;
        }

        RaycastHit hit;
        bool modified = false;

        float hor_dis = 0.0f;

        if (camera_pos.y < targetCenter.y)
        {
            var testPt = camera_pos;
            testPt.y = targetCenter.y;
            if (Physics.Raycast(testPt, Vector3.down, out hit, 50))/*这个检测必须进行，不能完全指望后面的检测，否则会有微小的显示问题。一般发生在摄像机贴近地面跑动时*/
            {
                if (camera_pos.y < hit.point.y + 0.5)/*偏移0.5.防止过于接近地面，并且在地面上面的情况，会因为摄像机近截面问题。导致显示地下的内容*/
                {
                    //modified = true;

                    hor_dis = Vector3.Distance(targetCenter, new Vector3(camera_pos.x, targetCenter.y, camera_pos.z));
                    camera_pos = hit.point;
                    camera_pos.y = (slope > 0.95f) ? hit.point.y : (camera_pos.y + hor_dis / maxDistance);
                    //摄像头在脚下的时候，hor_dis几乎为0
                    modified = false;
                    //Debug.Log("hit down.....camera_pos : " +camera_pos);		
                }
            }
        }

        //if (modified)
        //{
        //    hor_dis = Vector3.Distance(targetCenter, new Vector3(camera_pos.x, targetCenter.y, camera_pos.z));
        //    camera_pos = hit.point;
        //    camera_pos.y = (slope > 0.95f) ? hit.point.y : (camera_pos.y + hor_dis / maxDistance);
        //    //摄像头在脚下的时候，hor_dis几乎为0
        //    modified = false;
        //    //Debug.Log("hit down.....camera_pos : " +camera_pos);		
        //}

        var real_dis = Vector3.Distance(targetCenter, camera_pos);
        var direction = camera_pos - targetCenter;

        if (Physics.Raycast(targetCenter, direction, out hit, real_dis) && hit.collider.gameObject != gameObject)
        {
            //		modified = false;
            //		if(hit.collider.bounds.size.magnitude <= 15) {
            //			modified = false;	
            //		} else if (hit.collider.gameObject.tag == "bridge") {
            //			camera_pos.y = camera_pos.y + 2.5;
            //		} else if (hit.collider.gameObject.tag == "through"){
            //			modified = false;
            //		} else {
            //			modified = true;
            //		}
            //		Debug.LogError(hit.point.y < targetHead.y);
            camera_pos = hit.point;
            if (hit.point.y < targetHead.y)
            {
                camera_pos.y = targetHead.y;
                //			Debug.LogError(camera_pos);
            }
        }
        //	
        //	if(modified)
        //	{	
        //		hor_dis  = Vector3.Distance(targetCenter,Vector3(camera_pos.x,targetCenter.y,camera_pos.z));			
        //		camera_pos   = hit.point;
        //		camera_pos.y = (slope > 0.95)?hit.point.y:(camera_pos.y + hor_dis/maxDistance);/*摄像头在脚下的时候，hor_dis几乎为0*/	
        //	}	
        cameraTransform.position = camera_pos;
        

        var offsetToCenter = targetHead - cameraTransform.position;
        cameraTransform.rotation = Quaternion.LookRotation(new Vector3(offsetToCenter.x, offsetToCenter.y, offsetToCenter.z));
        Debug.DrawLine(targetHead, camera_pos, Color.red);
    }

    void EventMouseClicked()
    {
        //	Debug.LogError(Input.mousePosition);
        Vector3 mousePos = Input.mousePosition;
        Ray ray;
        ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hitInfo;
        Transform cameraTran;
        cameraTran = Camera.main.transform;
        if (Input.GetMouseButtonDown(0))
        {
          
            if (Physics.Raycast(ray, out hitInfo, 20f, (1 << 9)))
            {
       

                //Debug.LogError(hitInfo.transform.gameObject.layer);
                //			curPicturePos = hitInfo.point;
                //			curPicturePos = hitInfo.transform.Find("CameraPos").position;
                //			curPictureRotation = hitInfo.transform.Find("CameraPos").rotation;
                curPictureTran = hitInfo.transform.Find("CameraPos");
                //Debug.Log("hitInfo.transform.gameObject: " + hitInfo.transform.gameObject);
                //Debug.Log("curPictureTran: " + curPictureTran);
                bSeePicture = !bSeePicture;
                //Debug.Log("bSeePicture:"+ bSeePicture);

                if (bSeePicture)
                {
                    //GetComponent<ThirdPersonController>().enabled = false;
                    oldCameraRotation = Camera.main.transform.rotation;
                    GetComponent<ThirdPersonController>().isControllable = false;
                }
                else
                {
                    GetComponent<ThirdPersonController>().isControllable = true;
                    Camera.main.transform.rotation = oldCameraRotation;
                    //GetComponent<ThirdPersonController>().enabled = true;
                    needRefreshCameraPos = true;
                    //Cut(_target, centerOffset);
                }
            }
        }
    }

    void LateUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            RaycastHit hit2;
            
            var testPt = cameraTransform.position;
            testPt.y = 50;

            if (Physics.Raycast(testPt, Vector3.down, out hit2, 50))
            {
                Debug.Log("hit2.point.y : " + hit2.point.y);
            }
        }

        EventMouseClicked();

        if (!bSeePicture)
        {
            Apply(transform, Vector3.zero);
        }
        else
        {

            //		Camera.main.transform.position = transform.position;
            //		Camera.main.transform.position.y = curPicturePos.y;
            ////		Camera.main.transform.rotation = Quaternion.LookRotation(curPicturePos - Camera.main.transform.position);
            //		Camera.main.transform.rotation = transform.rotation;
            //		Camera.main.transform.position = curPicturePos;
            //		Camera.main.transform.rotation = curPictureRotation;
            if (curPictureTran != null)
            {
                Camera.main.transform.rotation = curPictureTran.rotation;
                Camera.main.transform.position = curPictureTran.position;
            }
        }
    }

    Vector3 GetCenterOffset()
    {
        return centerOffset;
    }
}
