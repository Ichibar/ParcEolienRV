using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using LockingPolicy = Thalmic.Myo.LockingPolicy;
using Pose = Thalmic.Myo.Pose;
using UnlockType = Thalmic.Myo.UnlockType;
using VibrationType = Thalmic.Myo.VibrationType;

public class BoatController : MonoBehaviour{

	// Myo game object to connect with.
	// This object must have a ThalmicMyo script attached.
	public GameObject myo = null;
	public Image run, stop, left, right; 

	// The pose from the last update. This is used to determine if the pose has changed
	// so that actions are only performed upon making them rather than every frame during
	// which they are active.
	private Pose _lastPose = Pose.Unknown;

    [SerializeField] private List<GameObject> m_motors;

	[SerializeField] private bool m_enableAudio = true;
	[SerializeField] private AudioSource m_boatAudioSource;
	[SerializeField] private float m_boatAudioMinPitch = 0.4F;
	[SerializeField] private float m_boatAudioMaxPitch = 1.2F;

	[SerializeField] public float m_FinalSpeed = 100F;
	[SerializeField] public float m_InertiaFactor = 0.005F;
	[SerializeField] public float m_turningFactor = 2.0F;
    [SerializeField] public float m_accelerationTorqueFactor = 35F;
	[SerializeField] public float m_turningTorqueFactor = 35F;

	private float m_verticalInput = 0F;
	private float m_horizontalInput = 0F;
    private Rigidbody m_rigidbody;
	private Vector2 m_androidInputInit;

	private float accel=0;
	private float accelBreak;
	private bool isRunning = false;


     void Start()  {
       	// base.Start();
        m_rigidbody = GetComponent<Rigidbody>();
      	// m_rigidbody.drag = 1;
      	//  m_rigidbody.angularDrag = 1;
	  	accelBreak = m_FinalSpeed*0.3f;

		left.canvasRenderer.SetAlpha(0.0f);
		right.canvasRenderer.SetAlpha(0.0f);
	}

	void Update()	{

		//setInputs (Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

		// Access the ThalmicMyo component attached to the Myo game object.
		ThalmicMyo thalmicMyo = myo.GetComponent<ThalmicMyo> ();

		// Check if the pose has changed since last update.
		// The ThalmicMyo component of a Myo game object has a pose property that is set to the
		// currently detected pose (e.g. Pose.Fist for the user making a fist). If no pose is currently
		// detected, pose will be set to Pose.Rest. If pose detection is unavailable, e.g. because Myo
		// is not on a user's arm, pose will be set to Pose.Unknown.
		if (thalmicMyo.pose != _lastPose) {
			_lastPose = thalmicMyo.pose;

			// Vibrate the Myo armband when a fist is made.
			if (thalmicMyo.pose == Pose.Fist) {
				thalmicMyo.Vibrate (VibrationType.Medium);
				Debug.Log ("fist");
				m_verticalInput = 4;
				isRunning = true;
			} else if (thalmicMyo.pose == Pose.WaveIn) {
				Debug.Log ("wave in");
				m_horizontalInput = -3;
				left.canvasRenderer.SetAlpha(1.0f);
				right.canvasRenderer.SetAlpha(0.0f);
			} else if (thalmicMyo.pose == Pose.WaveOut) {
				Debug.Log ("wave out");
				m_horizontalInput = 3;
				right.canvasRenderer.SetAlpha(1.0f);
				left.canvasRenderer.SetAlpha(0.0f);
			} else if (thalmicMyo.pose == Pose.DoubleTap) {
				Debug.Log ("double tap");
				m_verticalInput = 0;
				isRunning = false;
			} else if (thalmicMyo.pose == Pose.FingersSpread) {
				Debug.Log ("fingers spread");
				m_verticalInput = -3;
			} else {
				left.canvasRenderer.SetAlpha(0.0f);
				right.canvasRenderer.SetAlpha(0.0f);
				m_horizontalInput = 0;
			}
		}

		if (isRunning) {
			run.canvasRenderer.SetAlpha (1.0f);
			stop.canvasRenderer.SetAlpha(0.0f);
		} else {
			run.canvasRenderer.SetAlpha(0.0f);
			stop.canvasRenderer.SetAlpha(1.0f);
		}

	}

	public void setInputs(float iVerticalInput, float iHorizontalInput)	{
		m_verticalInput = iVerticalInput;
		m_horizontalInput = iHorizontalInput;
	}

	 void FixedUpdate()	{
		//base.FixedUpdate();

		if(m_verticalInput>0) {
			if(accel<m_FinalSpeed) { accel+=(m_FinalSpeed * m_InertiaFactor); accel*=m_verticalInput;}
		} else if(m_verticalInput==0) {
			if(accel>0) { accel-=m_FinalSpeed * m_InertiaFactor; }
			if(accel<0) { accel+=m_FinalSpeed * m_InertiaFactor; }
		}else if(m_verticalInput<0){
			if(accel>-accelBreak) { accel-=m_FinalSpeed * m_InertiaFactor*2;  }
		}
		
		m_rigidbody.AddRelativeForce(Vector3.forward  * accel);

        m_rigidbody.AddRelativeTorque(
			m_verticalInput * -m_accelerationTorqueFactor,
			m_horizontalInput * m_turningFactor,
			m_horizontalInput * -m_turningTorqueFactor
        );

        if(m_motors.Count > 0) {

            float motorRotationAngle = 0F;
			float motorMaxRotationAngle = 70;

			motorRotationAngle = - m_horizontalInput * motorMaxRotationAngle;

			for(int i=0; i<m_motors.Count; i++) {
				float currentAngleY = m_motors[i].transform.localEulerAngles.y;
				if (currentAngleY > 180.0f)
					currentAngleY -= 360.0f;

				float localEulerAngleY = Lerp(currentAngleY, motorRotationAngle, Time.deltaTime * 10);
				m_motors[i].transform.localEulerAngles = new Vector3(
					m_motors[i].transform.localEulerAngles.x,
					localEulerAngleY,
					m_motors[i].transform.localEulerAngles.z
				);
            }
        }
		
		if (m_enableAudio && m_boatAudioSource != null) 
		{
			
			float pitchLevel =  m_boatAudioMaxPitch*Mathf.Abs(m_verticalInput);
			if(m_verticalInput<0) pitchLevel*=0.7f;

			if (pitchLevel < m_boatAudioMinPitch) pitchLevel = m_boatAudioMinPitch;


			float smoothPitchLevel = Lerp(m_boatAudioSource.pitch, pitchLevel, Time.deltaTime*0.5f);

			m_boatAudioSource.pitch = smoothPitchLevel;
		}
    }

	static float Lerp (float from, float to, float value) {
		if (value < 0.0f) return from;
		else if (value > 1.0f) return to;
		return (to - from) * value + from;
	}

}
