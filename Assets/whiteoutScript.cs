using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class whiteoutScript : MonoBehaviour
{
	public KMAudio audio;
	public KMBombInfo bomb;
	public KMBossModule boss;
	public KMColorblindMode colorblindmode;

	private String[] sfxs = new String[]{"ok1", "ok2", "ok3"};

	public KMSelectable Screen;
	public GameObject StageIndicator;
	public GameObject ColorblindText;
	public Material[] Colors;
	private Color[] ColorsRGB = new Color[]
	{
		new Color(0, 0, 0),
		new Color(1, 0, 0),
		new Color(0, 1, 0),
		new Color(0, 0, 1),
		new Color(1, 1, 0),
		new Color(1, 0, 1),
		new Color(0, 1, 1),
		new Color(1, 1, 1),
	};
	private Color CurrentRGB = new Color(0, 0, 0);
	public Material[] MainTextColor;

	public static string[] ignoredModules = null;
	private int count = 0;
	private int ticker = 0;
	private int Stage = -1;
	private int curStage = 0;


	private bool IsWhiteout;
	private bool solving;
	private bool modulesolved;
	private bool animationPlaying;

	private bool colorblindModeEnabled;

	//logging
	static int moduleIdCounter = 1;
	int moduleId;

	public string TwitchHelpMessage = "Press the screen with !{0} push. Toggle colorblind mode with !{0} colorblind.";

	void Awake(){
		moduleId = moduleIdCounter++;

		Screen.OnInteract += delegate () { screenPress(); return false; };
	}

	void Start () {
		//The modules we ignore when getting the number of stages
		ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Whiteout", new string[]{
			"14",
			"42",
			"501",
			"A>N<D",
			"Bamboozling Time Keeper",
			"Brainf---",
			"Busy Beaver",
			"Forget Enigma",
			"Forget Everything",
			"Forget It Not",
			"Forget Me Later",
			"Forget Me Not",
			"Forget Perspective",
			"Forget The Colors",
			"Forget Them All",
			"Forget This",
			"Forget Us Not",
			"Iconic",
			"Kugelblitz",
			"Multitask",
			"OmegaForget",
			"Organization",
			"Password Destroyer",
			"Purgatory",
			"RPS Judging",
			"Simon Forgets",
			"Simon's Stages",
			"Souvenir",
			"Tallordered Keys",
			"The Heart",
			"The Swan",
			"The Time Keeper",
			"The Troll",
			"The Twin",
			"The Very Annoying Button",
			"Timing is Everything",
			"Turn The Key",
			"Whiteout",
			"Ultimate Custom Night",
			"Übermodule"
		});

		//Gets the number of stages for the module
		count = bomb.GetSolvableModuleNames().Where(x => !ignoredModules.Contains(x)).ToList().Count;

		colorblindModeEnabled = colorblindmode.ColorblindModeActive;
		ApplyColorblindToggle();
		
		StageProgression();
	}

	private void Update()
	{

		if(count < 0 || modulesolved)
		{
			return;
		}

		ticker++;
		if (ticker >= 5 && !animationPlaying)
		{
			ticker = 0;

			int check = bomb.GetSolvedModuleNames().Where(x => !ignoredModules.Contains(x)).Count();
			if (check != curStage)
			{
				if (IsWhiteout)
				{
					audio.PlaySoundAtTransform("aww",transform);
					StartCoroutine(WhiteoutAnimation("AWW..."));
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Whiteout #{0}] Strike! You didn't press the button before you solve the module.", moduleId);
					IsWhiteout = false;
				}
				else
				{
					audio.PlaySoundAtTransform(sfxs[UnityEngine.Random.Range(0, 3)],transform);
				}
				curStage = check;
				StageProgression();
			}
		}
	}

	void ApplyColorblindToggle()
	{
		if (colorblindModeEnabled)
		{
			ColorblindText.SetActive(true);
		}
		else
		{
			ColorblindText.SetActive(false);
		}
	}

	void StageProgression()
	{
		Stage++;
		if (count == 0)
		{
			for(int i=0;i<ColorsRGB.Length;i++)
			{
				if (rgbXOR(CurrentRGB, Color.white) == ColorsRGB[i])
				{
					if (i == 7)
					{
						StageIndicator.GetComponent<TextMesh>().color = MainTextColor[1].color;
						ColorblindText.GetComponent<TextMesh>().color = MainTextColor[1].color;
					}
					else
					{
						StageIndicator.GetComponent<TextMesh>().color = MainTextColor[0].color;
						ColorblindText.GetComponent<TextMesh>().color = MainTextColor[0].color;
					}
					StartCoroutine(ColorTransition(Colors[i].color));
					CurrentRGB = rgbXOR(CurrentRGB,ColorsRGB[i]);
					ColorblindText.GetComponent<TextMesh>().text = Colors[i].name;
					Debug.LogFormat("[Whiteout #{0}] The color of the screen on stage {1} is {2}.", moduleId, Stage, Colors[i].name);
					solving = true;
					count--;
				}
			}
		}
		else if (count > 0)
		{
			int randomColor = UnityEngine.Random.Range(0, 8);
			StartCoroutine(ColorTransition(Colors[randomColor].color));
			if (randomColor == 7)
			{
				StageIndicator.GetComponent<TextMesh>().color = MainTextColor[1].color;
				ColorblindText.GetComponent<TextMesh>().color = MainTextColor[1].color;
			}
			else
			{
				StageIndicator.GetComponent<TextMesh>().color = MainTextColor[0].color;
				ColorblindText.GetComponent<TextMesh>().color = MainTextColor[0].color;
			}
			CurrentRGB = rgbXOR(CurrentRGB,ColorsRGB[randomColor]);
			ColorblindText.GetComponent<TextMesh>().text = Colors[randomColor].name;

			Debug.LogFormat("[Whiteout #{0}] The color of the screen on stage {1} is {2}.", moduleId, Stage, Colors[randomColor].name);
			count--;
		}

		if (CurrentRGB == Color.white)
		{
			IsWhiteout = true;
		}

		StageIndicator.GetComponent<TextMesh>().text = Stage.ToString();
		Logging();
	}
	
	IEnumerator ColorTransition(Color targetColor)
	{
		animationPlaying = true;
		Color ColorDifs = (targetColor - Screen.GetComponent<MeshRenderer>().material.color)/25;
		for (int i = 0; i < 25; i++)
		{
			Screen.GetComponent<MeshRenderer>().material.color += ColorDifs;
			yield return new WaitForSeconds(0.005f);
		}
		animationPlaying = false;
		yield return null;
	}

	void Logging()
	{
		if (IsWhiteout)
		{
			Debug.LogFormat("[Whiteout #{0}] Your color is currently white(1,1,1). Whiteout!", moduleId);
		}
		else
		{
			for (int i = 0; i < ColorsRGB.Length; i++)
			{

				if (ColorsRGB[i] == CurrentRGB)
				{
					Debug.LogFormat("[Whiteout #{0}] Your color is currently {1}({2},{3},{4}).", moduleId, Colors[i].name, ColorsRGB[i].r, ColorsRGB[i].g, ColorsRGB[i].b);
				}
			}
		}
	}

	Color rgbXOR(Color x,Color y)
	{
		return new Color((x.r+y.r)%2,(x.g+y.g)%2,(x.b+y.b)%2);
	}

	void screenPress()
	{
		if (modulesolved || animationPlaying)
		{
			return;
		}

		Screen.AddInteractionPunch(.5f);

		if (!IsWhiteout)
		{
			if (CurrentRGB == Color.black)
			{
				StartCoroutine(WhiteoutAnimation("BLACKOUT?"));
				audio.PlaySoundAtTransform("blackout",transform);
			}
			else if (CurrentRGB == Color.red)
			{
				StartCoroutine(WhiteoutAnimation("REDOUT?"));
				audio.PlaySoundAtTransform("redout",transform);
			}
			else if (CurrentRGB == Color.green)
			{
				StartCoroutine(WhiteoutAnimation("GREENOUT?"));
				audio.PlaySoundAtTransform("greenout",transform);
			}
			else if (CurrentRGB == Color.blue)
			{
				StartCoroutine(WhiteoutAnimation("BLUEOUT?"));
				audio.PlaySoundAtTransform("blueout",transform);
			}
			else if (CurrentRGB == new Color(1, 1, 0))
			{
				StageIndicator.GetComponent<TextMesh>().characterSize = 0.0013F;
				StartCoroutine(WhiteoutAnimation("YELLOWOUT?"));
				audio.PlaySoundAtTransform("yellowout",transform);
			}
			else if (CurrentRGB == Color.magenta)
			{
				StageIndicator.GetComponent<TextMesh>().characterSize = 0.0012F;
				StartCoroutine(WhiteoutAnimation("MAGENTAOUT?"));
				audio.PlaySoundAtTransform("magentaout",transform);
			}
			else if (CurrentRGB == Color.cyan)
			{
				StartCoroutine(WhiteoutAnimation("CYANOUT?"));
				audio.PlaySoundAtTransform("cyanout",transform);
			}
			
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[Whiteout #{0}] Strike! You pressed button at the wrong moment.", moduleId);
		}
		else
		{
			IsWhiteout = false;
			Debug.LogFormat("[Whiteout #{0}] Screen pressed as well!", moduleId);
			StartCoroutine(WhiteoutAnimation("WHITEOUT!"));
			audio.PlaySoundAtTransform("whiteout",transform);

			if (solving)
			{
				modulesolved = true;
				GetComponent<KMBombModule>().HandlePass();
				Debug.LogFormat("[Whiteout #{0}] You passed every stage, Module solved.", moduleId);
			}
			//point...etc
		}
	}

	IEnumerator WhiteoutAnimation(String word)
	{
		animationPlaying = true;
		for (int i = 0; i < word.Length; i++)
		{
			StageIndicator.GetComponent<TextMesh>().text = word.Substring(0, i + 1);
			yield return new WaitForSeconds(0.32f/word.Length);
		}
		yield return new WaitForSeconds(0.5f);
		StageIndicator.GetComponent<TextMesh>().characterSize = 0.0015F;
		StageIndicator.GetComponent<TextMesh>().text = Stage.ToString();
		animationPlaying = false;
	}

	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] cutInBlank = command.Split(new char[] {' '});
		bool invalidcom = false;
		bool pressIt = false;

		if (cutInBlank.Length > 1)
		{
			invalidcom = true;
		}

		for (int i = 0; i < cutInBlank.Length; i++)
		{
			if (cutInBlank[i].Equals("PUSH", StringComparison.InvariantCultureIgnoreCase))
			{
				pressIt = true;
			}
			else if (cutInBlank[i].Equals("COLORBLIND", StringComparison.InvariantCultureIgnoreCase))
			{
				colorblindModeEnabled = colorblindModeEnabled ^ true;
				ApplyColorblindToggle();
				yield return null;
			}
			else
			{
				invalidcom = true;
			}
		}

		if (!invalidcom && pressIt)
		{
			Screen.OnInteract();
			yield return null;
		}
	}

	void TwitchHandleForcedSolve()
	{
		modulesolved = true;
		GetComponent<KMBombModule>().HandlePass();
	}

}
