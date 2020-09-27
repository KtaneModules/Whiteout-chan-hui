using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class whiteoutScript : MonoBehaviour
{

	struct RGB
	{
		public int R;
		public int G;
		public int B;

		public RGB(int R, int G, int B)
		{
			this.R = R;
			this.G = G;
			this.B = B;
		}
	};

	public KMAudio audio;
	public KMBombInfo bomb;
	public KMBossModule boss;

	public KMSelectable Screen;
	public Material[] Colors;
	private RGB[] ColorsRGB = new RGB[]
	{
		new RGB(0, 0, 0), 
		new RGB(1, 0, 0), 
		new RGB(0, 1, 0), 
		new RGB(0, 0, 1), 
		new RGB(1, 1, 0), 
		new RGB(1, 0, 1), 
		new RGB(0, 1, 1), 
		new RGB(1, 1, 1), 
	};
	private RGB CurrentRGB = new RGB(0, 0, 0);

	public static string[] ignoredModules = null;
	private int count = 0;
	private int ticker = 0;
	private int curStage = 0;
	

	private bool IsWhiteout;
	private bool solving;
	private bool modulesolved;

	//logging
	static int moduleIdCounter = 1;
	int moduleId;
	
	public string TwitchHelpMessage = "Press the screen with !{0} push.";

	void Awake(){
		moduleId = moduleIdCounter++;

		Screen.OnInteract += delegate () { screenPress(); return false; };
	}
	
	void Start () {
		//The modules we ignore when getting the number of stages
		ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Whiteout", new string[]{
			"14",
			//"42",
			//"501",
			"A>N<D",
			//"Bamboozling Time Keeper",
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
			//"Multitask",
			"OmegaForget",
			"Organization",
			//"Password Destroyer",
			"Purgatory",
			"RPS Judging",
			"Simon Forgets",
			"Simon's Stages",
			"Souvenir",
			"Tallordered Keys",
			//"The Heart",
			//"The Swan",
			//"The Time Keeper",
			//"The Troll",
			"The Twin",
			//"The Very Annoying Button",
			//"Timing is Everything",
			//"Turn The Key",
			"Whiteout",
			"Ultimate Custom Night",
			"Übermodule"
		});

		//Gets the number of stages for the module
		count = bomb.GetSolvableModuleNames().Where(x => !ignoredModules.Contains(x)).ToList().Count;

		StageProgression();
	}

	private void Update()
	{
		
		if(count < 0 || modulesolved)
		{
			return;
		}

		ticker++;
		if (ticker == 5)
		{
			ticker = 0;
			
			int check = bomb.GetSolvedModuleNames().Where(x => !ignoredModules.Contains(x)).Count();
			if (check != curStage) 
			{ 
				if (IsWhiteout)
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Whiteout #{0}] Strike! You didn't press the button before you solve the module.", moduleId);
					IsWhiteout = false;
				}
				curStage = check;
				audio.PlaySoundAtTransform("newSt",transform);
				StageProgression();
			}
		}
	}

	void StageProgression()
	{
		if (count == 0)
		{
			for(int i=0;i<ColorsRGB.Length;i++)
			{
				if (1 - CurrentRGB.R == ColorsRGB[i].R && 1 - CurrentRGB.G == ColorsRGB[i].G && 1 - CurrentRGB.B == ColorsRGB[i].B)
				{
					Screen.GetComponent<MeshRenderer>().material = Colors[i];
					CurrentRGB = rgbXOR(CurrentRGB,ColorsRGB[i]);
					Debug.LogFormat("[Whiteout #{0}] The color of the screen is {1}.", moduleId, Colors[i].name);
					solving = true;
					count--;
				}
			}
		}
		else if (count > 0)
		{
			int randomColor = UnityEngine.Random.Range(0, 8);
			Screen.GetComponent<MeshRenderer>().material = Colors[randomColor];
			CurrentRGB = rgbXOR(CurrentRGB,ColorsRGB[randomColor]);
			
			Debug.LogFormat("[Whiteout #{0}] The color of the screen is {1}.", moduleId, Colors[randomColor].name);
			count--;
		}

		if (CurrentRGB.R == 1 && CurrentRGB.G == 1 && CurrentRGB.B == 1)
		{
			IsWhiteout = true;
		}

		Logging();
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
			
				if (ColorsRGB[i].R == CurrentRGB.R && ColorsRGB[i].G == CurrentRGB.G && ColorsRGB[i].B == CurrentRGB.B)
				{
					Debug.LogFormat("[Whiteout #{0}] Your color is currently {1}({2},{3},{4}).", moduleId, Colors[i].name, ColorsRGB[i].R, ColorsRGB[i].G, ColorsRGB[i].B);
				}
			}
		}
	}

	RGB rgbXOR(RGB x,RGB y)
	{
		return new RGB((x.R+y.R)%2,(x.G+y.G)%2,(x.B+y.B)%2);
	}

	void screenPress()
	{
		if (modulesolved)
		{
			return;
		}
		
		Screen.AddInteractionPunch(.5f);

		if (!IsWhiteout)
		{
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[Whiteout #{0}] Strike! You pressed button at the wrong moment.", moduleId);
		}
		else
		{
			IsWhiteout = false;
			Debug.LogFormat("[Whiteout #{0}] Screen pressed as well!", moduleId);
			audio.PlaySoundAtTransform("actionL",transform);

			if (solving)
			{
				modulesolved = true;
				GetComponent<KMBombModule>().HandlePass();
				Debug.LogFormat("[Whiteout #{0}] You passed every stage, Module solved.", moduleId);
			}
			//point...etc
		}
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
