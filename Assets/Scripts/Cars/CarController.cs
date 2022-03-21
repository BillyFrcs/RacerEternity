using System.Collections.Generic;
using UnityEngine;

namespace Cars
{
    public enum ControlGameMode
    {
        KEYBOARD = 1,
        TOUCH = 2
    }

    [System.Serializable]
    public class CarSetting
    {
        public bool showNormalGizmos = false;

        public Transform carSteer;

        public HitGround[] hitGround;

        public List<Transform> cameraSwitchView;

        // public Transform[] cameraSwitchView;

        public float springs = 25000.0f;
        public float dampers = 1500.0f;

        public float carPower = 100f;
        public float nitroPower = 150f;
        public float brakePower = 8000f;

        public Vector3 shiftCentre = new Vector3(0.0f, -0.8f, 0.0f);

        public float maxSteerAngle = 25.0f;

        public float shiftDownRPM = 1500.0f;
        public float shiftUpRPM = 2500.0f;
        public float idleRPM = 500.0f;

        public float stiffness = 2.0f;

        public bool automaticGear = true;

        public float[] gears = {-10f, 9f, 6f, 4.5f, 3f, 2.5f};

        public float LimitBackwardSpeed = 60.0f;
        public float LimitForwardSpeed = 220.0f;
    }

    [System.Serializable]
    public class HitGround
    {
        public string tag;
        public bool grounded = false;

        public AudioClip brakeSound;
        public AudioClip groundSound;

        public Color brakeColor;
    }

    [System.Serializable]
    public struct CarParticles
    {
        public GameObject brakeParticlePrefab;

        public ParticleSystem shiftParticle1;
        public ParticleSystem shiftParticle2;
        public ParticleSystem shiftParticle3;

        // private GameObject[] wheelParticle = new GameObject[4];
    }

    [System.Serializable]
    public struct CarSounds
    {
        public AudioSource IdleEngine;
        public AudioSource LowEngine;
        public AudioSource HighEngine;

        public AudioSource nitro;
        public AudioSource switchGear;
    }

    [System.Serializable]
    public class CarLights
    {
        public Light[] brakeLights;
        public Light[] reverseLights;
    }

    [System.Serializable]
    public struct CarWheels
    {
        public ConnectWheel wheels;

        public WheelSetting setting;
    }

    [System.Serializable]
    public class ConnectWheel
    {
        public bool frontWheelDrive = true;

        public Transform frontRight;
        public Transform frontLeft;

        public bool backWheelDrive = true;

        public Transform backRight;
        public Transform backLeft;
    }

    [System.Serializable]
    public class WheelSetting
    {
        public float Radius = 0.4f;
        public float Weight = 1000.0f;
        public float Distance = 0.2f;
    }

    public class WheelComponent
    {
        public Transform wheel;
        public WheelCollider collider;
        public Vector3 startPos;

        public float rotation = 0.0f;
        public float rotation2 = 0.0f;
        public float posY = 0.0f;

        public float maxSteer;
        public bool drive;
    }

    public class CarController : MonoBehaviour
    {
        public ControlGameMode controlMode = ControlGameMode.KEYBOARD;

        public bool activeControl = false;

        public static CarController InstanceCarController;

        // Wheels Setting
        public CarWheels carWheels;

        // Lights Setting
        public CarLights carLights;

        // Car sounds
        public CarSounds carSounds;

        // Car Particle
        public CarParticles carParticles;

        // Car Engine Setting
        public CarSetting carSetting;

        private float _steer = 0f;
        private float _accel = 0.0f;

        [HideInInspector] public bool brake;

        private bool _shiftMotor;

        [HideInInspector] public float curTorque = 100f;
        [HideInInspector] public float powerShift = 100;
        [HideInInspector] public bool shift;

        // private float torque = 100f;

        [HideInInspector] public float speed = 0.0f;

        private float _lastSpeed = -10.0f;

        // private bool shifting = false;

        private float[] _efficiencyTable =
        {
            0.6f, 0.65f, 0.7f, 0.75f, 0.8f, 0.85f, 0.9f, 1.0f, 1.0f, 0.95f, 0.80f, 0.70f, 0.60f, 0.5f, 0.45f, 0.40f, 0.36f, 0.33f, 0.30f, 0.20f, 0.10f, 0.05f
        };

        private readonly float _efficiencyTableStep = 250.0f;

        private float _pitchSound;
        private float _pitchDelay;

        private float _shiftTime = 0.0f;

        private float _shiftDelay = 0.0f;

        [HideInInspector] public int currentGear = 0;
        [HideInInspector] public bool NeutralGear = true;

        [HideInInspector] public float motorRPM = 0.0f;

        [HideInInspector] public bool Backward = false;

        // TouchMode (Control)

        [HideInInspector] public float accelFwd = 0.0f;
        [HideInInspector] public float accelBack = 0.0f;
        [HideInInspector] public float steerAmount = 0.0f;
        
        private float _wantedRPM = 0.0f;
        private float w_rotate;
        private float _slip;
        private float _slip2 = 0.0f;

        private GameObject[] _Particle = new GameObject[4];

        private Vector3 _SteerCurAngle;

        private Rigidbody _MyRigidbody;
        
        private WheelComponent[] _Wheels;

        private float _target;

        private WheelComponent SetWheelComponent(Transform wheel, float maxSteer, bool drive, float yPosition)
        {
            WheelComponent Result = new WheelComponent();

            GameObject WheelCol = new GameObject(wheel.name + "WheelCollider");

            WheelCol.transform.parent = transform;
            WheelCol.transform.position = wheel.position;
            WheelCol.transform.eulerAngles = transform.eulerAngles;

            // Warning
            yPosition = WheelCol.transform.localPosition.y;

            WheelCol.AddComponent(typeof(WheelCollider));

            // WheelCollider col = (WheelCollider)wheelCol.AddComponent(typeof(WheelCollider));

            Result.wheel = wheel;
            Result.collider = WheelCol.GetComponent<WheelCollider>();
            Result.drive = drive;
            Result.posY = yPosition;
            Result.maxSteer = maxSteer;
            Result.startPos = WheelCol.transform.localPosition;

            return Result;
        }
        
        private void SetCarWheelsSettingComponent()
        {
            if (carSetting.automaticGear)
            {
                NeutralGear = false;
            }

            _MyRigidbody = transform.GetComponent<Rigidbody>();

            _Wheels = new WheelComponent[4];

            // Front wheels
            _Wheels[0] = SetWheelComponent(carWheels.wheels.frontRight, carSetting.maxSteerAngle, carWheels.wheels.frontWheelDrive, carWheels.wheels.frontRight.position.y);
            _Wheels[1] = SetWheelComponent(carWheels.wheels.frontLeft, carSetting.maxSteerAngle, carWheels.wheels.frontWheelDrive, carWheels.wheels.frontLeft.position.y);

            // Back wheels
            const float MAX_STEER = 0f;

            _Wheels[2] = SetWheelComponent(carWheels.wheels.backRight, MAX_STEER, carWheels.wheels.backWheelDrive, carWheels.wheels.backRight.position.y);
            _Wheels[3] = SetWheelComponent(carWheels.wheels.backLeft, MAX_STEER, carWheels.wheels.backWheelDrive, carWheels.wheels.backLeft.position.y);

            if (carSetting.carSteer)
            {
                _SteerCurAngle = carSetting.carSteer.localEulerAngles;
            }

            foreach (WheelComponent w in _Wheels)
            {
                WheelCollider col = w.collider;

                col.suspensionDistance = carWheels.setting.Distance;

                JointSpring js = col.suspensionSpring;

                js.spring = carSetting.springs;
                js.damper = carSetting.dampers;

                col.suspensionSpring = js;

                col.radius = carWheels.setting.Radius;

                col.mass = carWheels.setting.Weight;

                WheelFrictionCurve fc = col.forwardFriction;

                fc.asymptoteValue = 5000.0f;
                fc.extremumSlip = 2.0f;
                fc.asymptoteSlip = 20.0f;
                fc.stiffness = carSetting.stiffness;

                col.forwardFriction = fc;

                fc = col.sidewaysFriction;
                fc.asymptoteValue = 7500.0f;
                fc.asymptoteSlip = 2.0f;
                fc.stiffness = carSetting.stiffness;

                col.sidewaysFriction = fc;
            }
        }

        // Start in inspector panel hierarchy
        private void Awake()
        {
            SetCarWheelsSettingComponent();
        }

        // Start is called before the first frame update
        private void Start()
        {
            // Using this for instance static object
            if (InstanceCarController == null)
            {
                InstanceCarController = this;
            }

            // Check for shader model 4.5 or better support
            if (SystemInfo.graphicsShaderLevel >= 45)
            {
                Debug.Log("Woohoo, decent shaders supported!");
            }
            else
            {
                Debug.Log("Your device does not support shaders!");
            }

            /* Find camera object view
            foreach (Transform cameraView in GameObject.Find("View1").transform)
            {
                carSetting.cameraSwitchView.Add(cameraView);
            }
            */
        }
        
        // Update is called once per frame
        private void Update()
        {
            if (!carSetting.automaticGear && activeControl)
            {
                if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    ShiftUp();

                    // Debug.Log("Page up");
                }

                if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    ShiftDown();

                    // Debug.Log("Page down");
                }

                /* If we using this we should make the method to obsolete object
                if (Input.GetKeyDown("page up"))
                {
                    ShiftUp();
                }
    
                if (Input.GetKeyDown("page down"))
                {
                    ShiftDown();
                }
                */
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Application.Quit();

                Debug.LogAssertionFormat("Quit!");
            }
        }

        [System.Obsolete]
        private void FixedUpdate()
        {
            // Speed car
            SpeedOfCar();

            // Active control input mode
            ActiveControlMode();

            if (!carWheels.wheels.frontWheelDrive && !carWheels.wheels.backWheelDrive)
            {
                _accel = 0.0f;
            }

            if (carSetting.carSteer)
            {
                carSetting.carSteer.localEulerAngles = new Vector3(_SteerCurAngle.x, _SteerCurAngle.y, _SteerCurAngle.z + (_steer * -120.0f));
            }

            const float MAX_SPEED = 5.0f;

            if (carSetting.automaticGear && currentGear == 1 && _accel < 0.0f)
            {
                if (speed < MAX_SPEED)
                {
                    ShiftDown();
                }
            }
            else if (carSetting.automaticGear && currentGear == 0 && _accel > 0.0f)
            {
                if (speed < MAX_SPEED)
                {
                    ShiftUp();
                }
            }
            else if (carSetting.automaticGear && motorRPM > carSetting.shiftUpRPM && _accel > 0.0f && speed > 10.0f && !brake)
            {
                ShiftUp();
            }
            else if (carSetting.automaticGear && motorRPM < carSetting.shiftDownRPM && currentGear > 1)
            {
                ShiftDown();
            }

            if (speed < 1.0f)
            {
                Backward = true;
            }

            if (currentGear == 0 && Backward == true)
            {
                // carSetting.shiftCentre.z = -accel / -5;

                if (speed < carSetting.gears[0] * -10f)
                {
                    _accel = -_accel;
                }
            }
            else
            {
                Backward = false;

                // if (currentGear > 0)
                // carSetting.shiftCentre.z = -(accel / currentGear) / -5;
            }

            // carSetting.shiftCentre.x = -Mathf.Clamp(steer * (speed / 100), -0.03f, 0.03f);

            // Brake Lights
            foreach (Light brakeLight in carLights.brakeLights)
            {
                if (brake || _accel < 0 || speed < 1.0f)
                {
                    brakeLight.intensity = Mathf.MoveTowards(brakeLight.intensity, 8f, 0.5f);
                }
                else
                {
                    brakeLight.intensity = Mathf.MoveTowards(brakeLight.intensity, 0f, 0.5f);
                }

                // brakeLight.enabled = brakeLight.intensity == 0 ? false : true;

                brakeLight.enabled = brakeLight.intensity != 0;
            }

            // Reverse Lights
            foreach (Light WLight in carLights.reverseLights)
            {
                if (speed > 2.0f && currentGear == 0)
                {
                    WLight.intensity = Mathf.MoveTowards(WLight.intensity, 8f, 0.5f);
                }
                else
                {
                    WLight.intensity = Mathf.MoveTowards(WLight.intensity, 0f, 0.5f);
                }

                // WLight.enabled = WLight.intensity == 0 ? false : true;

                WLight.enabled = WLight.intensity != 0;
            }

            _wantedRPM = 5500.0f * _accel * 0.1f + _wantedRPM * 0.9f;

            float rpm = 0.0f;
            int motorizedWheels = 0;

            bool floorContact = false;
            int currentWheel = 0;

            foreach (WheelComponent w in _Wheels)
            {
                WheelCollider col = w.collider;

                if (w.drive)
                {
                    if (!NeutralGear && brake && currentGear < 2)
                    {
                        rpm += _accel * carSetting.idleRPM;

                        /*
                        if (rpm > 1)
                        {
                            carSetting.shiftCentre.z = Mathf.PingPong(Time.time * (accel * 10), 2.0f) - 1.0f;
                        }
                        else
                        {
                            carSetting.shiftCentre.z = 0.0f;
                        }
                        */
                    }
                    else
                    {
                        if (!NeutralGear)
                        {
                            rpm += col.rpm;
                        }
                        else
                        {
                            rpm += carSetting.idleRPM * _accel;
                        }
                    }

                    motorizedWheels++;
                }

                if (brake || _accel < 0.0f)
                {
                    if (_accel < 0.0f || brake && (w == _Wheels[2] || w == _Wheels[3]))
                    {
                        if (brake && _accel > 0.0f)
                        {
                            _slip = Mathf.Lerp(_slip, 5.0f, _accel * 0.01f);
                        }
                        else if (speed > 1.0f)
                        {
                            _slip = Mathf.Lerp(_slip, 1.0f, 0.002f);
                        }
                        else
                        {
                            _slip = Mathf.Lerp(_slip, 1.0f, 0.02f);
                        }

                        _wantedRPM = 0.0f;

                        col.brakeTorque = carSetting.brakePower;

                        w.rotation = w_rotate;
                    }
                }
                else
                {
                    col.brakeTorque = _accel == 0 || NeutralGear ? col.brakeTorque = 1000f : col.brakeTorque = 0;

                    _slip = speed > 0.0f ? (speed > 100 ? _slip = Mathf.Lerp(_slip, 1.0f + Mathf.Abs(_steer), 0.02f) : _slip = Mathf.Lerp(_slip, 1.5f, 0.02f)) : _slip = Mathf.Lerp(_slip, 0.01f, 0.02f);

                    w_rotate = w.rotation;
                }

                WheelFrictionCurve fc = col.forwardFriction;

                fc.asymptoteValue = 5000.0f;
                fc.extremumSlip = 2.0f;
                fc.asymptoteSlip = 20.0f;
                fc.stiffness = carSetting.stiffness / (_slip + _slip2);

                col.forwardFriction = fc;

                fc = col.sidewaysFriction;

                fc.stiffness = carSetting.stiffness / (_slip + _slip2);

                fc.extremumSlip = 0.2f + Mathf.Abs(_steer);

                col.sidewaysFriction = fc;

                if (shift && currentGear > 1 && speed > 50.0f && _shiftMotor && Mathf.Abs(_steer) < 0.2f)
                {
                    if (powerShift == 0)
                    {
                        _shiftMotor = false;
                    }

                    powerShift = Mathf.MoveTowards(powerShift, 0.0f, Time.deltaTime * 10.0f);

                    carSounds.nitro.volume = Mathf.Lerp(carSounds.nitro.volume, 1.0f, Time.deltaTime * 10.0f);

                    if (!carSounds.nitro.isPlaying)
                    {
                        carSounds.nitro.GetComponent<AudioSource>().Play();
                    }

                    curTorque = powerShift > 0 ? carSetting.nitroPower : carSetting.carPower;

                    // Nitro particle system
                    const float TIME = 10.0f;

                    carParticles.shiftParticle1.emissionRate = Mathf.Lerp(carParticles.shiftParticle1.emissionRate, powerShift > 0 ? 50 : 0, Time.deltaTime * TIME);
                    carParticles.shiftParticle2.emissionRate = Mathf.Lerp(carParticles.shiftParticle2.emissionRate, powerShift > 0 ? 50 : 0, Time.deltaTime * TIME);

                    if (carParticles.shiftParticle3 != null)
                    {
                        carParticles.shiftParticle3.emissionRate = Mathf.Lerp(carParticles.shiftParticle3.emissionRate, powerShift > 0 ? 50 : 0, Time.deltaTime * TIME);
                    }
                }
                else
                {
                    if (powerShift > 20)
                    {
                        _shiftMotor = true;
                    }

                    carSounds.nitro.volume = Mathf.MoveTowards(carSounds.nitro.volume, 0.0f, Time.deltaTime * 2.0f);

                    if (carSounds.nitro.volume == 0)
                    {
                        carSounds.nitro.Stop();
                    }

                    const float TARGET = 100f;

                    powerShift = Mathf.MoveTowards(powerShift, TARGET, Time.deltaTime * 5.0f);

                    curTorque = carSetting.carPower;

                    // Nitro particle system (need another implementation instead of using obsolete object)
                    carParticles.shiftParticle1.emissionRate = Mathf.Lerp(carParticles.shiftParticle1.emissionRate, 0f, Time.deltaTime * 10.0f);
                    carParticles.shiftParticle2.emissionRate = Mathf.Lerp(carParticles.shiftParticle2.emissionRate, 0f, Time.deltaTime * 10.0f);

                    if (carParticles.shiftParticle3 != null)
                    {
                        carParticles.shiftParticle3.emissionRate = Mathf.Lerp(carParticles.shiftParticle3.emissionRate, 0f, Time.deltaTime * 10.0f);
                    }
                }

                w.rotation = Mathf.Repeat(w.rotation + Time.deltaTime * col.rpm * 360.0f / 60.0f, 360.0f);
                w.rotation2 = Mathf.Lerp(w.rotation2, col.steerAngle, 0.1f);

                w.wheel.localRotation = Quaternion.Euler(w.rotation, w.rotation2, 0.0f);

                Vector3 lp = w.wheel.localPosition;

                if (col.GetGroundHit(out WheelHit hit))
                {
                    if (carParticles.brakeParticlePrefab)
                    {
                        if (_Particle[currentWheel] == null)
                        {
                            _Particle[currentWheel] = Instantiate(carParticles.brakeParticlePrefab, w.wheel.position, Quaternion.identity) as GameObject;

                            _Particle[currentWheel].name = "WheelParticle";
                            _Particle[currentWheel].transform.parent = transform;

                            _Particle[currentWheel].AddComponent<AudioSource>();
                            _Particle[currentWheel].GetComponent<AudioSource>().maxDistance = 50;
                            _Particle[currentWheel].GetComponent<AudioSource>().spatialBlend = 1;
                            _Particle[currentWheel].GetComponent<AudioSource>().dopplerLevel = 5;
                            _Particle[currentWheel].GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Custom;
                        }

                        var pc = _Particle[currentWheel].GetComponent<ParticleSystem>();

                        bool WGrounded = false;

                        for (int i = 0; i < carSetting.hitGround.Length; i++)
                        {
                            if (hit.collider.CompareTag(carSetting.hitGround[i].tag))
                            {
                                WGrounded = carSetting.hitGround[i].grounded;

                                if ((brake || Mathf.Abs(hit.sidewaysSlip) > 0.5f) && speed > 1)
                                {
                                    _Particle[currentWheel].GetComponent<AudioSource>().clip = carSetting.hitGround[i].brakeSound;
                                }
                                else if (_Particle[currentWheel].GetComponent<AudioSource>().clip != carSetting.hitGround[i].groundSound && !_Particle[currentWheel].GetComponent<AudioSource>().isPlaying)
                                {
                                    _Particle[currentWheel].GetComponent<AudioSource>().clip = carSetting.hitGround[i].groundSound;
                                }

                                _Particle[currentWheel].GetComponent<ParticleSystem>().startColor = carSetting.hitGround[i].brakeColor;
                            }
                        }

                        if (WGrounded && speed > 5 && !brake)
                        {
                            pc.enableEmission = true;

                            _Particle[currentWheel].GetComponent<AudioSource>().volume = 0.5f;

                            if (!_Particle[currentWheel].GetComponent<AudioSource>().isPlaying)
                            {
                                _Particle[currentWheel].GetComponent<AudioSource>().Play();
                            }
                        }
                        else if ((brake || Mathf.Abs(hit.sidewaysSlip) > 0.6f) && speed > 1)
                        {
                            if (_accel < 0.0f || (brake || Mathf.Abs(hit.sidewaysSlip) > 0.6f) && (w == _Wheels[2] || w == _Wheels[3]))
                            {
                                if (!_Particle[currentWheel].GetComponent<AudioSource>().isPlaying)
                                {
                                    _Particle[currentWheel].GetComponent<AudioSource>().Play();
                                }

                                pc.enableEmission = true;

                                _Particle[currentWheel].GetComponent<AudioSource>().volume = 10;
                            }
                        }
                        else
                        {
                            pc.enableEmission = false;

                            _Particle[currentWheel].GetComponent<AudioSource>().volume = Mathf.Lerp(_Particle[currentWheel].GetComponent<AudioSource>().volume, 0f, Time.deltaTime * 10.0f);
                        }
                    }

                    lp.y -= Vector3.Dot(w.wheel.position - hit.point, transform.TransformDirection(0f, 1f, 0f) / transform.lossyScale.x) - (col.radius);
                    lp.y = Mathf.Clamp(lp.y, -10.0f, w.posY);

                    floorContact = floorContact || w.drive;
                }
                else
                {
                    if (_Particle[currentWheel] != null)
                    {
                        var pc = _Particle[currentWheel].GetComponent<ParticleSystem>();

                        pc.enableEmission = false;
                    }

                    lp.y = w.startPos.y - carWheels.setting.Distance;

                    _MyRigidbody.AddForce(Vector3.down * 5000.0f);
                }

                currentWheel++;

                w.wheel.localPosition = lp;
            }

            if (motorizedWheels > 1)
            {
                // Warning
                rpm = rpm /= motorizedWheels;
            }

            motorRPM = 0.95f * motorRPM + 0.05f * Mathf.Abs(rpm * carSetting.gears[currentGear]);

            if (motorRPM > 5500.0f)
            {
                motorRPM = 5200.0f;
            }

            int index = (int) (motorRPM / _efficiencyTableStep);

            if (index >= _efficiencyTable.Length && index < 0)
            {
                index = _efficiencyTable.Length - 1;

                // Debug.Log("Called " + index);
            }

            if (index < 0)
            {
                index = 0;
            }

            float newTorque = curTorque * carSetting.gears[currentGear] * _efficiencyTable[index];

            foreach (WheelComponent w in _Wheels)
            {
                WheelCollider col = w.collider;

                if (w.drive)
                {
                    if (Mathf.Abs(col.rpm) > Mathf.Abs(_wantedRPM))
                    {
                        col.motorTorque = 0f;
                    }
                    else
                    {
                        float curTorqueCol = col.motorTorque;

                        if (!brake && _accel != 0 && NeutralGear == false)
                        {
                            if (speed < carSetting.LimitForwardSpeed && currentGear > 0 || speed < carSetting.LimitBackwardSpeed && currentGear == 0)
                            {
                                col.motorTorque = curTorqueCol * 0.9f + newTorque * 1.0f;
                            }
                            else
                            {
                                col.motorTorque = 0f;

                                col.brakeTorque = 2000f;
                            }
                        }
                        else
                        {
                            col.motorTorque = 0f;
                        }
                    }
                }

                if (brake || _slip2 > 2.0f)
                {
                    col.steerAngle = Mathf.Lerp(col.steerAngle, _steer * w.maxSteer, 0.02f);
                }
                else
                {
                    float steerAngleCar = Mathf.Clamp(speed / carSetting.maxSteerAngle, 1.0f, carSetting.maxSteerAngle);

                    col.steerAngle = _steer * w.maxSteer / steerAngleCar;
                }
            }

            // Setting pitch car 
            PitchCarSetting();
        }

        private void ShiftUp()
        {
            float now = Time.timeSinceLevelLoad;

            if (now < _shiftDelay)
                return;

            if (currentGear < carSetting.gears.Length - 1)
            {
                // if (!carSounds.switchGear.isPlaying)

                carSounds.switchGear.GetComponent<AudioSource>().Play();

                if (!carSetting.automaticGear)
                {
                    if (currentGear == 0)
                    {
                        if (NeutralGear)
                        {
                            currentGear++;

                            NeutralGear = false;
                        }
                        else
                        {
                            NeutralGear = true;
                        }
                    }
                    else
                    {
                        currentGear++;
                    }
                }
                else
                {
                    currentGear++;
                }

                _shiftDelay = now + 1.0f;

                _shiftTime = 1.5f;
            }
        }

        private void ShiftDown()
        {
            float now = Time.timeSinceLevelLoad;

            if (now < _shiftDelay)
                return;

            if (currentGear > 0 || NeutralGear)
            {
                // if (!carSounds.switchGear.isPlaying)

                carSounds.switchGear.GetComponent<AudioSource>().Play();

                if (!carSetting.automaticGear)
                {
                    if (currentGear == 1)
                    {
                        if (!NeutralGear)
                        {
                            currentGear--;

                            NeutralGear = true;
                        }
                    }
                    else if (currentGear == 0)
                    {
                        NeutralGear = false;
                    }
                    else
                    {
                        currentGear--;
                    }
                }
                else
                {
                    currentGear--;
                }

                _shiftDelay = now + 0.1f;

                _shiftTime = 2.0f;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.root.GetComponent<CarController>())
            {
                collision.transform.root.GetComponent<CarController>()._slip2 = Mathf.Clamp(collision.relativeVelocity.magnitude, 0.0f, 10.0f);

                _MyRigidbody.angularVelocity = new Vector3(-_MyRigidbody.angularVelocity.x * 0.5f, _MyRigidbody.angularVelocity.y * 0.5f, -_MyRigidbody.angularVelocity.z * 0.5f);

                _MyRigidbody.velocity = new Vector3(_MyRigidbody.velocity.x, _MyRigidbody.velocity.y * 0.5f, _MyRigidbody.velocity.z);
            }
        }

        void OnCollisionStay(Collision collision)
        {
            if (collision.transform.root.GetComponent<CarController>())
            {
                collision.transform.root.GetComponent<CarController>()._slip2 = 5.0f;
            }
        }

        private void ActiveControlMode()
        {
            if (activeControl)
            {
                if (controlMode == ControlGameMode.KEYBOARD)
                {
                    _accel = 0.0f;

                    brake = false;
                    shift = false;

                    if (carWheels.wheels.frontWheelDrive || carWheels.wheels.backWheelDrive)
                    {
                        const float MAX_DELTA_KEYBOARD = 0.2f;

                        _target = Input.GetAxis("Horizontal");

                        _steer = Mathf.MoveTowards(_steer, _target, MAX_DELTA_KEYBOARD);

                        _accel = Input.GetAxis("Vertical");
                        brake = Input.GetButton("Jump");

                        // Active nitroPower when press shift key
                        shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                        /* Camera shift
                        if (shift)
                        {
                            CameraController.InstanceCameraController.distance = 7f;
    
                            // Debug.Log("Shift");
                        }
                        */
                    }

                    /* Camera shift
                    if (shift == false)
                    {
                        CameraController.InstanceCameraController.distance = 6f;
    
                        Debug.Log("Not Shift");
                    }
                    */
                }
                else if (controlMode == ControlGameMode.TOUCH)
                {
                    if (accelFwd != 0f)
                    {
                        _accel = accelFwd;
                    }
                    else
                    {
                        _accel = accelBack;
                    }

                    const float MAX_DELTA_TOUCH = 0.07f;

                    _steer = Mathf.MoveTowards(_steer, steerAmount, MAX_DELTA_TOUCH);
                }
            }
            else
            {
                _accel = 0.0f;
                _steer = 0.0f;

                brake = false;
                shift = false;
            }
        }

        private void SpeedOfCar()
        {
            // speed of car
            speed = _MyRigidbody.velocity.magnitude * 2.7f;

            if (speed < _lastSpeed - 10f && _slip < 10f)
            {
                _slip = _lastSpeed / 15f;
            }

            _lastSpeed = speed;

            if (_slip2 != 0.0f)
            {
                _slip2 = Mathf.MoveTowards(_slip2, 0.0f, 0.1f);
            }

            _MyRigidbody.centerOfMass = carSetting.shiftCentre;
        }

        private void PitchCarSetting()
        {
            // Calculate pitch (keep it within reasonable bounds)
            _pitchSound =
                Mathf.Clamp(1.2f + (motorRPM - carSetting.idleRPM) / (carSetting.shiftUpRPM - carSetting.idleRPM), 1.0f, 10.0f);

            _shiftTime = Mathf.MoveTowards(_shiftTime, 0.0f, 0.1f);

            // if (_pitchSound == 1f) // Warning message
            if (Mathf.Abs(_pitchSound) <= 1f)
            {
                carSounds.IdleEngine.volume = Mathf.Lerp(carSounds.IdleEngine.volume, 1.0f, 0.1f);
                carSounds.LowEngine.volume = Mathf.Lerp(carSounds.LowEngine.volume, 0.5f, 0.1f);
                carSounds.HighEngine.volume = Mathf.Lerp(carSounds.HighEngine.volume, 0.0f, 0.1f);

                // Debug.Log("Pitch: " + System.Math.Abs(_pitchSound));
            }
            else
            {
                carSounds.IdleEngine.volume = Mathf.Lerp(carSounds.IdleEngine.volume, 1.8f - _pitchSound, 0.1f);

                if ((_pitchSound > _pitchDelay || _accel > 0) && _shiftTime == 0.0f)
                {
                    carSounds.LowEngine.volume = Mathf.Lerp(carSounds.LowEngine.volume, 0.0f, 0.2f);
                    carSounds.HighEngine.volume = Mathf.Lerp(carSounds.HighEngine.volume, 1.0f, 0.1f);
                }
                else
                {
                    carSounds.LowEngine.volume = Mathf.Lerp(carSounds.LowEngine.volume, 0.5f, 0.1f);
                    carSounds.HighEngine.volume = Mathf.Lerp(carSounds.HighEngine.volume, 0.0f, 0.2f);
                }

                carSounds.HighEngine.pitch = _pitchSound;
                carSounds.LowEngine.pitch = _pitchSound;

                _pitchDelay = _pitchSound;
            }
        }

        /////////////// Show Normal Gizmos ////////////////////////////
        private void OnDrawGizmos()
        {
            if (!carSetting.showNormalGizmos || Application.isPlaying)
                return;

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Gizmos.matrix = rotationMatrix;
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

            Gizmos.DrawCube(Vector3.up / 1.5f, new Vector3(2.5f, 2.0f, 6.0f));
            Gizmos.DrawSphere(carSetting.shiftCentre / transform.lossyScale.x, 0.2f);
        }
        
        private void OnGUI()
        {
            GUI.TextField(new Rect(10f, 10f, 200f, 100f), gameObject.name, 100);

            GUI.Label(new Rect(15f, 30f, 100f, 100f), $"Motor RPM {Mathf.Floor(motorRPM)}");
            GUI.Label(new Rect(15f, 50f, 500f, 500f), $"RPM {Mathf.Floor(_wantedRPM)}");
            GUI.Label(new Rect(15f, 70f, 500f, 500f), $"Km/h {Mathf.Floor(speed)}");
            GUI.Label(new Rect(15f, 90f, 500f, 500f), $"Gear {currentGear}");
        }
    }
}
