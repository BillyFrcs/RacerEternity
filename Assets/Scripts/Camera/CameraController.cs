using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Cars;
using Lights;

namespace CameraGame
{
    [System.Serializable]
    public class CarUIClass
    {
        public Image tachometerNeedle;
        public Image barShiftGUI;

        public Text speedText;
        public Text GearText;
    }

    public class CameraController : MonoBehaviour
    {
        public Transform TargetCar;
        // private Transform TargetCar;

        public float smooth = 0.5f;
        public float distance = 5.0f;
        public float height = 1.0f;
        public float angle = 20.0f;

        [HideInInspector] public List<Transform> cameraSwitchView;
        // [HideInInspector] public Transform[] cameraSwitchView;

        public static CameraController InstanceCameraController;

        public LayerMask lineOfSightMask = 0;

        // public CarUIClass CarUI;

        private float yVelocity = 0.0f;
        private float xVelocity = 0.0f;

        [HideInInspector] public int Switch;

        // private int gearst = 0;

        // private float thisAngle = -150;

        private float restTime = 0.0f;

        private Rigidbody myRigidbody;

        private CarController _CarController;

        private int PLValue = 0;

        ////////////////////////////////////////////// TouchMode (Control) ////////////////////////////////////////////////////////////////////

        private void PoliceLightSwitch()
        {
            if (!TargetCar.gameObject.GetComponent<PoliceLightsCarSystem>())
                return;

            PLValue++;

            if (PLValue > 1)
            {
                PLValue = 0;
            }

            if (PLValue == 1)
            {
                TargetCar.gameObject.GetComponent<PoliceLightsCarSystem>().activeLight = true;
            }

            if (PLValue == 0)
            {
                TargetCar.gameObject.GetComponent<PoliceLightsCarSystem>().activeLight = false;
            }
        }

        public void CameraSwitch()
        {
            Switch++;

            if (Switch > cameraSwitchView.Count)
            {
                Switch = 0;
            }
        }

        public void CarAccelForward(float amount)
        {
            _CarController.accelFwd = amount;
        }

        public void CarAccelBack(float amount)
        {
            _CarController.accelBack = amount;
        }

        public void CarSteer(float amount)
        {
            _CarController.steerAmount = amount;
        }

        public void CarHandBrake(bool HBrakeing)
        {
            _CarController.brake = HBrakeing;
        }

        public void CarShift(bool Shifting)
        {
            _CarController.shift = Shifting;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void RestCar()
        {
            if (restTime == 0)
            {
                const float valueCarForce = 700000f;

                myRigidbody.AddForce(Vector3.up * valueCarForce);

                myRigidbody.MoveRotation(Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

                /* Use this only for debugging purposes
                if (restTime >= 0)
                {
                     Debug.LogAssertionFormat("Car FOrce active");
                }
                */

                restTime = 2.0f;
            }
        }

        /*
        public void ShowCarUI()
        {
            gearst = carScript.currentGear;
            CarUI.speedText.text = ((int)carScript.speed).ToString();
    
            if (carScript.carSetting.automaticGear)
            {
                if (gearst > 0 && carScript.speed > 1)
                {
                    CarUI.GearText.color = Color.green;
                    CarUI.GearText.text = gearst.ToString();
                }
                else if (carScript.speed > 1)
                {
                    CarUI.GearText.color = Color.red;
                    CarUI.GearText.text = "R";
                }
                else
                {
                    CarUI.GearText.color = Color.white;
                    CarUI.GearText.text = "N";
                }
            }
            else
            {
                if (carScript.NeutralGear)
                {
                    CarUI.GearText.color = Color.white;
                    CarUI.GearText.text = "N";
                }
                else
                {
                    if (carScript.currentGear != 0)
                    {
                        CarUI.GearText.color = Color.green;
                        CarUI.GearText.text = gearst.ToString();
                    }
                    else
                    {
                        CarUI.GearText.color = Color.red;
                        CarUI.GearText.text = "R";
                    }
                }
    
            }
    
            thisAngle = (carScript.motorRPM / 20) - 175;
            thisAngle = Mathf.Clamp(thisAngle, -180, 90);
    
            CarUI.tachometerNeedle.rectTransform.rotation = Quaternion.Euler(0, 0, -thisAngle);
            CarUI.barShiftGUI.rectTransform.localScale = new Vector3(carScript.powerShift / 100.0f, 1, 1);
        }
        */

        // Start is called before the first frame update
        private void Start()
        {
            _CarController = (CarController) TargetCar.GetComponent<CarController>();

            myRigidbody = TargetCar.GetComponent<Rigidbody>();

            cameraSwitchView = _CarController.carSetting.cameraSwitchView;

            if (InstanceCameraController == null)
            {
                InstanceCameraController = this;
            }

            /* Uncomment this line if we didn't reference the target object
            if (_CarController != null)
            {
                _CarController = (CarController)TargetCar.GetComponent<CarController>();
            }
    
            if (myRigidbody != null)
            {
                myRigidbody = TargetCar.GetComponent<Rigidbody>();
            }
    
            try
            {
                TargetCar = GameObject.FindGameObjectWithTag(CameraTag.playerCar).transform;
            }
            catch
            {
                TargetCar = null;
            }
    
            try
            {
                cameraSwitchView = _CarController.carSetting.cameraSwitchView;
    
                // cameraSwitchView = _CarController.carSetting.cameraSwitchView;
            }
            catch
            {
                cameraSwitchView = null;
            }
            */
        }

        // Update is called once per frame
        private void Update()
        {
            if (!TargetCar)
                return;

            _CarController = (CarController) TargetCar.GetComponent<CarController>();

            CarInput();

            SwitchCarCamera();
        }

        private void CarInput()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                RestCar();
            }

            /*
            if (Input.GetKeyDown(KeyCode.R))
            {
                Application.LoadLevel(Application.loadedLevel);
            }
            */

            if (Input.GetKeyDown(KeyCode.E))
            {
                PoliceLightSwitch();
            }

            if (restTime != 0.0f)
            {
                restTime = Mathf.MoveTowards(restTime, 0.0f, Time.deltaTime);
            }

            GetComponent<Camera>().fieldOfView = Mathf.Clamp(_CarController.speed / 10.0f + 60.0f, 60.0f, 90.0f);

            if (Input.GetKeyDown(KeyCode.C))
            {
                Switch++;

                if (Switch > cameraSwitchView.Count)
                {
                    Switch = 0;
                }
            }
        }

        private void SwitchCarCamera()
        {
            if (Switch == 0)
            {
                // Damp angle from current y-angle towards target y-angle

                float xAngle = Mathf.SmoothDampAngle(transform.eulerAngles.x, TargetCar.eulerAngles.x + angle, ref xVelocity, smooth);

                float yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, TargetCar.eulerAngles.y, ref yVelocity, smooth);

                // Look at the target
                transform.eulerAngles = new Vector3(xAngle, yAngle, 0.0f);

                var direction = transform.rotation * -Vector3.forward;

                var targetDistance = AdjustLineOfSight(TargetCar.position + new Vector3(0, height, 0), direction);

                transform.position = TargetCar.position + new Vector3(0, height, 0) + direction * targetDistance;
            }
            else
            {
                /*
                transform.position = cameraSwitchView[Switch - 1].position;
    
                transform.rotation = Quaternion.Lerp(transform.rotation, cameraSwitchView[Switch - 1].rotation, Time.deltaTime * 5.0f);
                */

                // Optimize
                transform.SetPositionAndRotation(cameraSwitchView[Switch - 1].position, Quaternion.Lerp(transform.rotation, cameraSwitchView[Switch - 1].rotation, Time.deltaTime * 5.0f));
            }
        }

        private float AdjustLineOfSight(Vector3 target, Vector3 direction)
        {
            // RaycastHit hit;

            // if (Physics.Raycast(target, direction, out hit, distance, lineOfSightMask.value))

            // Optimize
            if (Physics.Raycast(target, direction, out RaycastHit hit, lineOfSightMask.value))
            {
                return hit.distance;
            }
            else
            {
                return distance;
            }
        }
    }
}
