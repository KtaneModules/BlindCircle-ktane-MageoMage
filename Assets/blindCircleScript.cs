using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit; //gets edgework and stuff

public class blindCircleScript : MonoBehaviour {

	public KMAudio audio; //audio script
	public KMBombInfo bomb; //bomb info script

	//for logging:
	static int moduleIdCounter = 1;
	int modId;
	private bool modSolved; //default is false

	
	public KMSelectable[] wedges;
	public MeshRenderer[] WedgeRenderers;
	public MeshRenderer[] MainRenderers;
	public TextMesh[] WedgeTextMeshes;
	public TextMesh InfoText;
	public Color[] wedgeClrs;
	public GameObject Circle;
	/*
			"CC7308", //orange
			"1911F3", //blue
			"CC0000", //red
			"00FF21", //green
			"C9CC21", //yellow
			"FFFFFF", //white
			"121212", //black
			"CC15C6", //magenta
			"A0A0A0" //grey
	*/
	private readonly string[] NAMES = {"orange", "blue", "red",  "green", "yellow", "white", "black", "magenta"}; 

	private int state = 0;
	private bool counter;
	private bool spin = true;
	private int lit;
	private int xor;
	private int[] goalSequence;
	private int[][] solutionGrid;
	private int presses = 0;

	private bool colorblind = false;


	//called before Start()
	void Awake(){
		modId = moduleIdCounter++;

		foreach (KMSelectable wedge in wedges){
			KMSelectable temp = wedge;
			wedge.OnInteract += delegate(){Press(temp); return false;};
		}

		colorblind = GetComponent<KMColorblindMode>().ColorblindModeActive;
	}

	void Press(KMSelectable wedge){

		switch(state){
			case 0:
				audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
				Debug.LogFormat("[Blind Circle #{0}] Pressed {1} wedge", modId, wedge.GetComponentInChildren<TextMesh>().text);
				Debug.LogFormat("[Blind Circle #{0}] Starting Flash Sequence", modId);
				state = 1;
				StartCoroutine(Flash());
				presses = 0;
				break;
			
			case 2:
				audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
				Debug.LogFormat("[Blind Circle #{0}] Pressed {1} wedge", modId, wedge.GetComponentInChildren<TextMesh>().text);
				if (!modSolved && Array.IndexOf(wedges, wedge) == goalSequence[presses]){
					presses++;
					if (presses >= 3){
						GetComponent<KMBombModule>().HandlePass();
						modSolved = true;
						Debug.LogFormat("[Blind Circle #{0}] All Presses have been given correctly, module disarmed!", modId);
						spin = false;
						StartCoroutine(FadeAll(wedgeClrs[UnityEngine.Random.Range(0,8)], 2f, MainRenderers));
						StartCoroutine(SegmentFade(wedgeClrs[8], new Color(wedgeClrs[8].r, wedgeClrs[8].g, wedgeClrs[8].b, 0), 1f, lit));
						state = 3;
					}
				} else if (!modSolved){
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Blind Circle{0}] Incorrect press made, module struck and resets", modId);
					state = 1;
					StartCoroutine(Reset());
				}
				break;
			case 3:
				StartCoroutine(FadeAll(wedgeClrs[UnityEngine.Random.Range(0,8)], 0.5f, MainRenderers));

				break;
		}
	}

	private IEnumerator Flash(){
		int index;
			int[] flashInd = new int[3];
		for (int i = 0; i < 3; i++){
			index = UnityEngine.Random.Range(0,8);
			flashInd[i] = index;
			InfoText.text = NAMES[index];
			if (colorblind)
				StartCoroutine(InfoFade(wedgeClrs[6], 0.5f));
			StartCoroutine(FadeAll(wedgeClrs[index], 0.5f, WedgeRenderers));
			yield return StartCoroutine(FadeAll(wedgeClrs[index], 0.5f, MainRenderers));
			if (colorblind)
				StartCoroutine(InfoFade(new Color(wedgeClrs[8].r, wedgeClrs[8].g, wedgeClrs[8].b, 0), 0.5f));
			StartCoroutine(FadeAll(wedgeClrs[8], 0.5f, WedgeRenderers));
			yield return StartCoroutine(FadeAll(wedgeClrs[8], 0.5f, MainRenderers));
		}
		Debug.LogFormat("[Blind Circle #{0}] The flashes in order are: {1}, {2}, {3}", modId, NAMES[flashInd[0]], NAMES[flashInd[1]], NAMES[flashInd[2]]);
		lit = UnityEngine.Random.Range(0,8);
		if (colorblind)
			StartCoroutine(SegmentFade(wedgeClrs[lit], wedgeClrs[6], 1f, lit));
		else
			StartCoroutine(SegmentFade(wedgeClrs[lit], new Color(wedgeClrs[8].r, wedgeClrs[8].g, wedgeClrs[8].b, 0), 1f, lit));
		yield return goalSequence = GenerateSolution(flashInd);
		Debug.LogFormat("[Blind Circle #{0}] The expected input is: {1}, {2}, {3}; that is {4}, {5}, {6} wedges further clockwise", modId, NAMES[goalSequence[0]], NAMES[goalSequence[1]], NAMES[goalSequence[2]], (goalSequence[0]-lit+8)%8, (goalSequence[1]-lit+8)%8, (goalSequence[2]-lit+8)%8);
		state = 2;
	}

	private IEnumerator Reset(){
		presses = 0;
		yield return StartCoroutine(FadeCircle(wedgeClrs[8], new Color(wedgeClrs[8].r, wedgeClrs[8].g, wedgeClrs[8].b, 0), 1.5f));
		state = 0;
	}

	private int[] GenerateSolution(int[] colorInd){

		int[] temp = new int[8];
		for (int i = 0; i < 8; i++)
			temp[i] = 0;
		foreach (int index in colorInd)
			temp[index]++;
		int unique = 0;
		int max = 0;
		foreach(int count in temp){
			if (count > 0){
				unique++;
				max = count > max ? count : max; 
			}
		}
		if (counter){
			solutionGrid[1][5] = unique%8; //white, xor true
			solutionGrid[0][6] = (((-max)%8)+8)%8;	//black, xor false
		}else{
			solutionGrid[1][5] = (8-unique)%8; //white, xor true
			solutionGrid[0][6] = max %8;	//black, xor false
		}

		solutionGrid[0][0] = (8 + colorInd[0] - lit)%8; //orange, xor false
		solutionGrid[1][0] = (14 - lit)%8; //orange, xor true

		solutionGrid[0][1] = 7 - lit; //blue, xor false
		solutionGrid[1][1] = (8 + colorInd[2] - lit)%8; //blue, xor true

		solutionGrid[0][4] = (8 - lit)%8; //yellow, xor false

		solutionGrid[1][7] = (11 - lit)%8; //magenta, xor true


		int[] result = new int[3];
		for (int i = 0; i < 3; i++)
			result[i] = (solutionGrid[xor][colorInd[i]]+lit)%8;
		
		return result;
	}

	private IEnumerator FadeAll(Color newColor, float endTime, MeshRenderer[] meshs) //fades all the provided MeshRenderers
	{
		var rn = new List<Color>();
		foreach(MeshRenderer wedge in meshs)
			rn.Add(wedge.material.color);
		var startcolor = MainRenderers[0].material.color;

		var startTime = Time.time;

		while ((Time.time - startTime) < endTime)
		{
			var currentTime = (Time.time - startTime) / endTime;
			for (var i = 0; i < 8; i++)
			{
				meshs[i].material.color = Color.Lerp(rn[i], newColor, currentTime);
				MainRenderers[i].material.color = Color.Lerp(startcolor, newColor, currentTime);
			}

			yield return null;
		}

		for (var i = 0; i < 8; i++)
		{
			meshs[i].material.color = newColor;
			MainRenderers[i].material.color = newColor;
		}
	}

	private IEnumerator SegmentFade(Color newColor, Color textColor, float endTime, int index) //Fades the Outer Part and Text of a given segment
	{
		var startcolorWedge = WedgeRenderers[index].material.color;
		var startcolorText = WedgeTextMeshes[index].color;
		var startTime = Time.time;

		while ((Time.time - startTime) < endTime)
		{
			var currentTime = (Time.time - startTime) / endTime;
			WedgeRenderers[index].material.color = Color.Lerp(startcolorWedge, newColor, currentTime);
			if (colorblind)
				WedgeTextMeshes[index].color = Color.Lerp(startcolorText, textColor, currentTime);
			yield return null;
		}

		WedgeRenderers[index].material.color = newColor;
		WedgeTextMeshes[index].color = textColor;
	}

	private IEnumerator InfoFade(Color newColor, float endTime) //Fades the Collorblind Text
	{
		var startcolor = InfoText.color;
		var startTime = Time.time;

		while ((Time.time - startTime) < endTime)
		{
			var currentTime = (Time.time - startTime) / endTime;
			InfoText.color = Color.Lerp(startcolor, newColor, currentTime);
			yield return null;
		}
		InfoText.color = newColor;
	}


	// Use this for initialization
	void Start () {
		foreach(MeshRenderer wedge in WedgeRenderers)
			wedge.material.color = wedgeClrs[8];
		foreach(TextMesh text in WedgeTextMeshes)
			text.color = new Color(wedgeClrs[8].r, wedgeClrs[8].g, wedgeClrs[8].b, 0);
		InfoText.color = new Color(wedgeClrs[8].r, wedgeClrs[8].g, wedgeClrs[8].b, 0);

		Circle.transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360), 0); //random starting rotation

		counter = (UnityEngine.Random.Range(0,2) == 0);
		if (counter)
			Debug.LogFormat("[Blind Circle #{0}] The circle rotates counterclockwise", modId);
		else
			Debug.LogFormat("[Blind Circle #{0}] The circle rotates clockwise", modId);

		StartCoroutine(SpinCircle());

		xor = counter ^ bomb.GetIndicators().ToList().Count() >= 3 ? 1 : 0;

		solutionGrid = new int[2][];
		for (int i = 0; i < 2; i++)
			solutionGrid[i] = new int[8];

		//standard shifts and shifts that always stay the same -> order is not according to manual see up top (and basically everywhere else)
		if (counter){
			solutionGrid[0][2] = bomb.GetBatteryHolderCount()%8;	//red, xor false
			solutionGrid[1][2] = 7;		//red, xor true

			int temp = -bomb.GetPortCount();
			while (temp < 0)
				temp += 8;
			solutionGrid[0][3] = temp%8;	//green, xor false
			solutionGrid[1][3] = bomb.CountUniquePorts()%8;		//green, xor true
			
			solutionGrid[1][4] = 1;		//yellow, xor true
			
			solutionGrid[0][5] = bomb.GetBatteryCount()%8;	//white, xor false
		}else{
			int temp = -bomb.GetBatteryHolderCount();
			while (temp < 0)
				temp += 8;
			solutionGrid[0][2] = temp;	//red, xor false
			solutionGrid[1][2] = 1;		//red, xor true

			solutionGrid[0][3] = bomb.GetPortCount()%8;	//green, xor false
			temp = -bomb.CountUniquePorts();
			while (temp < 0)
				temp += 8;
			solutionGrid[1][3] = temp;		//green, xor true
			
			solutionGrid[1][4] = 7;		//yellow, xor true
			
			temp = -bomb.GetBatteryCount();
			while (temp < 0)
				temp += 8;
			solutionGrid[0][5] = temp;	//white, xor false
			
		}
		solutionGrid[0][7] = 5;	//magenta, xor false
		solutionGrid[1][6] = 2;	//black, xor true
	}

	
	private IEnumerator FadeCircle(Color newColor, Color textColor, float endTime)
	{
		var rnWedge = new List<Color>();
		foreach(MeshRenderer wedge in WedgeRenderers)
			rnWedge.Add(wedge.material.color);
		var rnText = new List<Color>();
		foreach(TextMesh text in WedgeTextMeshes)
			rnText.Add(text.color);
		
		var startTime = Time.time;

		while ((Time.time - startTime) < endTime)
		{
			var currentTime = (Time.time - startTime) / endTime;
			for (var i = 0; i < 8; i++)
			{
				WedgeRenderers[i].material.color = Color.Lerp(rnWedge[i], newColor, currentTime);
				WedgeTextMeshes[i].color = Color.Lerp(rnText[i], textColor, currentTime);
			}

			yield return null;
		}
		for (var i = 0; i < 8; i++)
			{
				WedgeRenderers[i].material.color =newColor;
				WedgeTextMeshes[i].color = textColor;
		}
	}

	private IEnumerator SpinCircle()
	{
		do
		{
			var framerate = 1f / Time.deltaTime;
			var rotation = 6f / framerate; //6 degrees per second.
			if (counter)
				rotation *= -1;

			var y = Circle.transform.localEulerAngles.y;
			y += rotation;
			Circle.transform.localEulerAngles = new Vector3(0, y, 0);

			yield return null;
		} while (spin);
	}
}
