﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Station : MonoBehaviour {

	[SerializeField] Streetcar streetcar;
	[SerializeField] GameObject scorePanel;
	[SerializeField] int scoreToAdd = 10;


	void OnTriggerEnter2D(Collider2D other) {

		if(other.CompareTag("Fare")) {

			streetcar.AddToScore(scoreToAdd);
			Destroy(other.gameObject);
		}
		else if (other.CompareTag("Raver")) {

			streetcar.AddToScore(2 * scoreToAdd);
			Destroy(other.gameObject);
		}

		scorePanel.GetComponentInChildren<Text>().text = "Score:" + streetcar.GetScore().ToString("000");
	}
}
