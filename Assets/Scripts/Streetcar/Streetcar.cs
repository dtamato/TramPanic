﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Streetcar : MonoBehaviour {

	[Header("Parameters")]
	[SerializeField] float acceleration = 0.001f;
	[SerializeField] float maxSpeed = 0.1f;
	public int maxPassengers;
	public float passengerLeaveRate;
	public int inspectorCount;
	public Text speedBoostUI;
	public GameObject[] CapacityCount;

	[Header("Score")]
	public static int score;
	public GameObject scorePanel;

	[Header("Audio")]
	public AudioClip pickupSound;
	public AudioClip coinSound;
	public AudioClip fartSound;
	public AudioClip slowSound;
	public AudioClip stunSound;
	public AudioClip speedSound;
	public AudioClip immuneSound;
	public AudioClip raverSound;

	[Header("Passenger Info")]
	public List<Sprite> streetCarPassengers;
	public List<string> streetCarPassengersRole;
	public List<Role> abilityPassengers = new List<Role>(2);
	public GameObject pedestrian;

	[Header("References")]
	[SerializeField] Animator effectsAnimator;
	[SerializeField] GameObject streetcarCanvas;

    [Header("Minimap")]
    public GameObject minimapStreetCar;
	[SerializeField] Transform stationOneTransform;
	[SerializeField] Transform stationTwoTransform;
	[SerializeField] RectTransform miniStationOneTransform;
	[SerializeField] RectTransform miniStationTwoTransform;

	[Header("Ability Data")]
	[SerializeField] Sprite[] abilitiesSprites;
	public SpriteRenderer FirstAbilitySprite;
	public SpriteRenderer SecondAbilitySprite;

    private Rigidbody2D rb2d;
	private Animator streetcarAnimator;
	private ColorStrobe colorStrobe;
	private float moveSpeed = 0;
	private bool changingAcceleration = false;
	private float counter;
	private int currentPassengers = 0;
	private bool stationUp = false;
	private bool stationDown = false;
	private bool chunkyOnBoard = false;
	private bool inspectorOnBoard = false;
	private bool canMove = true;
	private GameController gameController;
	private MusicController musicController;
    private float raverBuffTime = 30;

    private bool scoreMultiplier = false;

    // Abilities
    public List<string> abilities = new List<string>(2);
	float firstTapTime = 0;
	string firstButtonHit = "";


	void Awake () {
		
        scoreMultiplier = false;
        rb2d = this.GetComponent<Rigidbody2D> ();
		streetcarAnimator = this.GetComponent<Animator>();
		colorStrobe = this.GetComponentInChildren<ColorStrobe>();
		streetCarPassengers = new List<Sprite>();
		streetCarPassengersRole = new List<string>();

		gameController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController>();
		musicController = GameObject.FindGameObjectWithTag("MusicController").GetComponent<MusicController>();
		speedBoostUI.text =  inspectorCount.ToString();
		FirstAbilitySprite = GameObject.Find ("AbilitySprite1").GetComponent<SpriteRenderer>();
		SecondAbilitySprite = GameObject.Find ("AbilitySprite2").GetComponent<SpriteRenderer> ();

		score = 0;
	}

	void Update () {

		if (Input.GetKeyDown (KeyCode.Alpha1)) 
		{
			abilities.Add ("Speed Boost");
			AbilitySpriteOrder ();
			inspectorCount++;
		}
		else if (Input.GetKeyDown (KeyCode.Alpha2)) 
		{
			abilities.Add ("Officer");
			AbilitySpriteOrder ();
		}

		if (Input.GetKeyDown (KeyCode.Space)) 
		{
			abilityControls ();
			AbilitySpriteOrder ();
		}

        if (scoreMultiplier == true)
        {
            raverBuffTime -= Time.deltaTime;
            
			if(raverBuffTime <= 0)
            {
                scoreMultiplier = false;
				colorStrobe.StopAllCoroutines();
				colorStrobe.GetComponent<SpriteRenderer>().color = Color.white;
				musicController.PlayRegularMusic();
            }
        }
	}

	void FixedUpdate () {
			
		if (canMove && gameController.GameStarted())
        {
            // Give streetcar friction if not inputting acceleration
            if (!changingAcceleration)
            {
                moveSpeed *= 0.9f;
            }

            // Move the streetcar
            rb2d.MovePosition(this.transform.position + (Vector3.right * moveSpeed));

            // Move minimap streetcar
			float percentageBetweenStations = this.transform.position.x / (stationTwoTransform.position.x - stationOneTransform.position.x);
			float newMinimapStreetCarX = percentageBetweenStations * (miniStationTwoTransform.localPosition.x - miniStationOneTransform.localPosition.x) + miniStationOneTransform.localPosition.x;
			minimapStreetCar.GetComponent<RectTransform>().localPosition = new Vector3(newMinimapStreetCarX, minimapStreetCar.GetComponent<RectTransform>().localPosition.y, 0);
        }

		// Check if can dropoff people
		if(Mathf.Abs(moveSpeed) < 0.01f) {
			
	        if (stationDown) {
				
				DropOffPassengers (-2);
			}
			else if (stationUp) {
				
				DropOffPassengers (2);
			}
		}
	}

	void OnCollisionEnter2D(Collision2D other) {

		if (other.gameObject.GetComponent<Pedestrian> ()) {

			Pedestrian collidedWith = other.gameObject.GetComponent<Pedestrian> ();
			//Debug.Log ("Hit by " + collidedWith.GetRole());

			if (collidedWith.GetRole() == Role.Raver)
			{
				scoreMultiplier = true;
				raverBuffTime = 30;

				GetComponent<AudioSource>().clip = raverSound;
				GetComponent<AudioSource>().Play ();

				colorStrobe.StartCoroutine(colorStrobe.RecursiveColorChange());

				musicController.PlayRaverMusic();
				Destroy(other.gameObject);
			}
			else if(currentPassengers < maxPassengers) {

				if (collidedWith.GetRole() == Role.Coin) {

					GetComponent<AudioSource>().clip = coinSound;
					GetComponent<AudioSource>().Play ();

					streetcarAnimator.SetTrigger("Grow");
				}
				else if (collidedWith.GetRole() == Role.Stink) {

					// Done so that the remove passenger audio doesn't cancel out this fart noise
					GameObject fartSoundGameobject = new GameObject();
					fartSoundGameobject.AddComponent<AudioSource>();
					fartSoundGameobject.GetComponent<AudioSource>().clip = fartSound;
					fartSoundGameobject = Instantiate(fartSoundGameobject, Vector3.zero, Quaternion.identity) as GameObject;
					fartSoundGameobject.GetComponent<AudioSource>().Play();
					Destroy(fartSoundGameobject, 1);

					streetcarAnimator.SetTrigger("Shrink");

					// Force out passengers
					int passengersToRemove = Random.value < 0.5f ? 3 : 4;
					for(int i = 0; i < passengersToRemove; i++) {

						int direction = (Random.value < 0.5f) ? -1 : 1;
						RemovePassenger(direction);
					}
				}
				else if (collidedWith.GetRole() == Role.Chunky) 
				{	
					chunkyOnBoard = true;

					GetComponent<AudioSource>().clip = slowSound;
					GetComponent<AudioSource>().Play ();

					if (inspectorOnBoard == false) 
					{
						maxSpeed = 0.06f;
					} 

					else if (inspectorOnBoard == true) 
					{
						maxSpeed = 0.1f;
						acceleration = 0.001f;
					}

					effectsAnimator.SetTrigger("Chunky");
				}
				else if (collidedWith.GetRole() == Role.Inspector) 
				{
					abilities.Add ("Speed Boost");
					AbilitySpriteOrder ();
					inspectorOnBoard = true;
					inspectorCount++;
					speedBoostUI.text =  inspectorCount.ToString();
					abilityPassengers.Add(Role.Inspector);
					GetComponent<AudioSource>().clip = pickupSound;
					GetComponent<AudioSource>().Play ();
				}
				else if (collidedWith.GetRole() == Role.Dazer)
				{
					StartCoroutine (TempDisableMovement ());

					// Done so that the remove passenger audio doesn't cancel out this fart noise
					GameObject dazerAudioGameObject = new GameObject();
					dazerAudioGameObject.AddComponent<AudioSource>();
					dazerAudioGameObject.GetComponent<AudioSource>().clip = stunSound;
					dazerAudioGameObject = Instantiate(dazerAudioGameObject, Vector3.zero, Quaternion.identity) as GameObject;
					dazerAudioGameObject.GetComponent<AudioSource>().Play();
					Destroy(dazerAudioGameObject, 1);

	                // Force out half of passenger count
	                int halfOfPassengers = (int)(0.5f * currentPassengers);
	                for (int i = 0; i < halfOfPassengers; i++) {

	                    int direction = (Random.value < 0.5f) ? -1 : 1;
	                    RemovePassenger(direction);
	                }
				}
				else if(collidedWith.GetRole() == Role.Officer)
				{
					GetComponent<AudioSource>().clip = pickupSound;
					GetComponent<AudioSource>().Play ();

					abilities.Add ("Officer");
					AbilitySpriteOrder ();
					abilityPassengers.Add(Role.Officer);
				}

				// Do actions that are universal to all relevant roles
				currentPassengers++;
				streetCarPassengers.Add(other.gameObject.GetComponent<SpriteRenderer>().sprite);
				streetCarPassengersRole.Add(other.gameObject.GetComponent<Pedestrian>().GetRoleString ());

				// Update capacity panel
				for (int i = 0; i < currentPassengers; i++) {
					
					CapacityCount[i].SetActive(true);
					CapacityCount[i].GetComponent<Animator>().SetTrigger("Pulse");
				}

				Destroy(other.gameObject);
			}

			streetcarAnimator.SetBool("Full", (currentPassengers == maxPassengers));
		}
		else if(other.transform.CompareTag("Barricade")) {

			Camera.main.GetComponent<CameraEffects>().ShakeCamera(0.05f);
		}
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.tag == "StationUp")
		{
			stationUp = true;
			stationDown = false;
		}

		if (other.gameObject.tag == "StationDown")
		{
			stationUp = false;
			stationDown = true;
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (other.gameObject.tag == "StationUp")
		{
			stationUp = false;
			stationDown = false;
		}

		if (other.gameObject.tag == "StationDown")
		{
			stationUp = false;
			stationDown = false;
		}
	}

	IEnumerator TempDisableMovement () {

		canMove = false;
		rb2d.bodyType = RigidbodyType2D.Static;
		effectsAnimator.SetTrigger("Dazer");
		this.GetComponent<SpriteRenderer>().color = Color.grey;
		colorStrobe.gameObject.GetComponent<SpriteRenderer>().color = Color.grey;
		Camera.main.GetComponent<CameraEffects> ().ShakeCamera ();

		yield return new WaitForSeconds (3);

		canMove = true;
		rb2d.bodyType = RigidbodyType2D.Dynamic;
		effectsAnimator.SetTrigger("Norm");
		this.GetComponent<SpriteRenderer>().color = Color.white;
		colorStrobe.gameObject.GetComponent<SpriteRenderer>().color = Color.white;
	}

	IEnumerator speedBoost()
	{	
		if (chunkyOnBoard == false) {

			maxSpeed = 0.175f;
			acceleration = 0.005f;
			inspectorCount--;
			speedBoostUI.text = inspectorCount.ToString ();
			Debug.Log (maxSpeed);
		}

		else 
		{
			maxSpeed = 0.15f;
			inspectorCount--;
			speedBoostUI.text = inspectorCount.ToString ();
			//Debug.Log (maxSpeed);
		}

		yield return new WaitForSeconds (2);

		if (chunkyOnBoard == false) {

			maxSpeed = 0.1f;
			acceleration = 0.001f;
		}
		else 
		{
			maxSpeed = 0.075f;
			acceleration = 0.001f;
		}
	}

	public void Accelerate () {

		changingAcceleration = true;
		if(moveSpeed < 0) { moveSpeed *= 0.9f; }
		moveSpeed += acceleration;
		moveSpeed = Mathf.Clamp (moveSpeed, -maxSpeed, maxSpeed);
	}

	public void Decelerate () {

		changingAcceleration = true;
		if(moveSpeed > 0) { moveSpeed *= 0.9f; }
		moveSpeed -= acceleration;
		moveSpeed = Mathf.Clamp (moveSpeed, -maxSpeed, maxSpeed);
	}

	public void EndAcceleration () {

		changingAcceleration = false;
	}

	public float GetMoveSpeed () {

		return moveSpeed;
	}

	public void TriggerAbility () {

		const float doubleTapTimeThreshold = 0.6f;

		// If first tap has already occurred
		if(firstTapTime != 0) {

			// Record second tap
			float secondTapTime = Time.time;
			string secondButtonHit = EventSystem.current.currentSelectedGameObject.name;

			// If double tap was fast enough
			if(secondTapTime - firstTapTime <= doubleTapTimeThreshold && firstButtonHit == secondButtonHit) {
				Debug.Log("double tap time: " + (secondTapTime - firstTapTime));
				abilityControls();
				AbilitySpriteOrder();

				firstTapTime = 0;
				secondTapTime = 0;
				firstButtonHit = "";
			}
			else {

				// Reset first tap
				firstTapTime = 0;
				firstButtonHit = "";
			}
		}
		else {

			// Record first tap
			firstTapTime = Time.time;
			firstButtonHit = EventSystem.current.currentSelectedGameObject.name;
		}
	}

	public void abilityControls()
	{
		if (abilities.IndexOf ("Speed Boost") == 0 /*&& inspectorCount > 0*/) 
		{	
			abilities.Remove ("Speed Boost");
			abilityPassengers.RemoveAt(abilityPassengers.Count - 1);

			GetComponent<AudioSource>().clip = speedSound;
			GetComponent<AudioSource>().Play ();

			StartCoroutine (speedBoost ());
		} 
		else if (abilities.IndexOf ("Officer") == 0) 
		{	
			abilities.Remove ("Officer");
			abilityPassengers.RemoveAt(abilityPassengers.Count - 1);

			GetComponent<AudioSource>().clip = immuneSound;
			GetComponent<AudioSource>().Play ();

			if(maxSpeed < 0.1f) { maxSpeed = 0.1f; }

			Camera.main.GetComponentInChildren<CameraOverlay>().ShowOverlay();
			GameObject[] allPedestrians = GameObject.FindGameObjectsWithTag("Pedestrian");

			foreach (GameObject pedestrianObject in allPedestrians) {

				Pedestrian pedestrian = pedestrianObject.GetComponent<Pedestrian>();

				if(pedestrian.GetRole() == Role.Stink || pedestrian.GetRole() == Role.Chunky || pedestrian.GetRole() == Role.Dazer) {

					Destroy(pedestrian.gameObject);
				}
			}

			effectsAnimator.SetTrigger("Norm");
		}
    }

    public void DropOffPassengers(int pedestrianDirection)
    {
        if (streetCarPassengers.Count > 0)
        {
            counter += Time.deltaTime;

            if (counter > passengerLeaveRate)
            {
                RemovePassenger(pedestrianDirection);
                //GetComponent<AudioSource>().clip = pickupSound;
                //GetComponent<AudioSource>().Play();
            }
        }
    }

    public void RemovePassenger(int pedestrianDirection)
    {
		if(streetCarPassengers.Count > 0) {

			int passengerIndex = streetCarPassengers.Count - 1;
            Vector3 spawnPosition = this.transform.position + new Vector3(Random.Range(-1f, 1f), 0.3f * pedestrianDirection, 0);
            GameObject pedestrianPrefab = Instantiate(pedestrian, spawnPosition, Quaternion.identity) as GameObject;
            pedestrianPrefab.tag = (scoreMultiplier) ? "Raver" : "Fare";

            pedestrianPrefab.GetComponent<SpriteRenderer>().sprite = streetCarPassengers[passengerIndex];
			pedestrianPrefab.GetComponent<SpriteRenderer>().sortingOrder = -pedestrianDirection * this.GetComponent<SpriteRenderer>().sortingOrder;
            pedestrianPrefab.GetComponent<Pedestrian>().SetDestination(this.transform.position + new Vector3(0, 2 * pedestrianDirection, 0));
            pedestrianPrefab.GetComponent<Pedestrian>().SetMoveSpeed(1.5f);
            pedestrianPrefab.GetComponent<Collider2D>().isTrigger = true;

            streetCarPassengers.RemoveAt(passengerIndex);
            streetCarPassengersRole.RemoveAt(passengerIndex);
            currentPassengers--;

            if (!streetCarPassengersRole.Contains("INSPECTOR"))
            {
                inspectorOnBoard = false;
                inspectorCount = 0;
                maxSpeed = 0.1f;
                acceleration = 0.001f;
                effectsAnimator.SetTrigger("Norm");
            }

            if (!streetCarPassengersRole.Contains("CHUNKY"))
            {
                chunkyOnBoard = false;
                maxSpeed = 0.1f;
                acceleration = 0.001f;
                effectsAnimator.SetTrigger("Norm");
            }

            counter = 0;
            GetComponent<AudioSource>().clip = pickupSound;
            GetComponent<AudioSource>().Play();

			CapacityCount[currentPassengers].SetActive(false);
			streetcarAnimator.SetBool("Full", (currentPassengers == maxPassengers));
		}
    }

    public void AbilitySpriteOrder()
	{
		if (abilities.Count.Equals (0)) 
		{
			FirstAbilitySprite.sprite = null;

		} 
		else if (abilities.IndexOf ("Speed Boost") == 0) 
		{
			FirstAbilitySprite.sprite = abilitiesSprites [0];

		} 
		else if (abilities.IndexOf ("Officer") == 0) 
		{
			FirstAbilitySprite.sprite = abilitiesSprites [1];
		}
        
		if (abilities.Count <= 1) 
		{
				SecondAbilitySprite.sprite = null;

		}
		else if (abilities.IndexOf ("Speed Boost") == 1 || abilities.IndexOf("Speed Boost") == 0 && abilities.LastIndexOf("Speed Boost") == 1) 
		{
			SecondAbilitySprite.sprite = abilitiesSprites [0];
			//Debug.Log ("SB1 Trigger");
		} 
		else if (abilities.IndexOf ("Officer") == 1 || abilities.IndexOf("Officer") == 0 && abilities.LastIndexOf("Officer") == 1)
		{
				SecondAbilitySprite.sprite = abilitiesSprites [1];
			//Debug.Log ("OFF1 Trigger");
		}
	}

	public void ShowStreetcarCanvas () {

		streetcarCanvas.SetActive(true);
	}

	public void AddToScore (int scoreAddition) {

		score += scoreAddition;
	}

	public int GetScore () {

		return score;
	}
}