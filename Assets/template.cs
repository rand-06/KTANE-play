using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class template : MonoBehaviour
{
    public KMAudio Audio;
    public AudioClip[] clips = new AudioClip[21];
    public AudioClip startSound;
    public AudioClip fastSound;
    public KMSelectable[] buttons = new KMSelectable[21];
    public GameObject[] buttonObjects = new GameObject[21];
    public GameObject[] startObjects = new GameObject[21];
    public GameObject[] stopObjects = new GameObject[21];
    public KMSelectable fastButton;
    public TextMesh seedText;
    public TextMesh screen;
    public GameObject play;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    private int move = 0;
    private int correct = 0;
    private string base64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz<>";
    private string binarySeed = "";
    private int[] starts;
    private int[] stops;
    private bool[] states = new bool[21] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

    private string[] coords = new string[16] {"A1","B1","A2","B2","C1","D1","C2","D2","A3","B3","A4","B4","C3","D3","C4","D4"};

    string toBase2(int num)
    {
        string ans = "";
        int mask = 32;
        for (int i=0; i<6; i++)
        {
            ans += ((num & mask) != 0) ? "1" : "0";
            mask >>= 1;
        }
        return ans;
    }

    int toBase10(string str)
    {
        switch (str)
        {
            case "00": case "0": return 0;
            case "01": case "1": return 1;
            case "10": return 2;
            case "11": return 3;
            default: return -1;
        }
    }

    int[] toOrder(string str)
    {
        int[] ans = new int[4];
        List<int> list = new List<int> { 0, 1, 2, 3 };
        int ind = toBase10(str.Substring(0, 2));
        ans[0] = list[ind];
        list.RemoveAt(ind);
        ind = toBase10(str.Substring(2, 2));
        ind = ind == 3 ? 0 : ind;
        ans[1] = list[ind];
        list.RemoveAt(ind);
        ind = toBase10(str[4].ToString());
        ans[2] = list[ind];
        list.RemoveAt(ind);
        ans[3] = list[0];
        return ans;
    }

    void generateSeed()
    {
        string seedString = "Seed: ";
        if (Rnd.Range(0, 2) == 1)
        {
            binarySeed += "1";
            seedString += "+";
        } else {
            binarySeed += "0";
            seedString += "-";
        }
        for (int i=0; i < 11; i++)
        {
            int digit = Rnd.Range(0, 64);
            digit = digit == 24 ? 0 : digit;
            binarySeed += toBase2(digit);
            seedString += base64[digit].ToString();
        }
        seedText.text = seedString;
    }

    void generateSolution()
    {
        starts = new int[21];
        stops = new int[21];
        starts[0] = 0;
        stops[0] = toBase10(binarySeed.Substring(0, 2)) + 1;
        int[] order1 = toOrder(binarySeed.Substring(2, 5));
        int count = 0;
        foreach (int i in order1)
        {
            starts[1 + i] = stops[0] + count + 1;
            stops[1 + i] = starts[1 + i] + toBase10(binarySeed.Substring(7 + 2 * i, 2)) + 1;
            count++;
        }
        for (int j=0; j<4; j++)
        {
            int[] order2 = toOrder(binarySeed.Substring(15+5*j, 5));
            //11ORDER11223344ORDERORDERORDERORDER11223344112233441122334411223344
            //0         1         2         3         4         5         6     6
            count = 0;
            foreach (int i in order2)
            {
                starts[5 + 4 * j + i] = stops[1 + j] + count + 1;
                stops[5 + 4 * j + i] = starts[5 + 4 * j + i] + toBase10(binarySeed.Substring(35+8*j+2*i, 2)) + 1;
                count++;
            }
        }
        string startslog = "Starts: [";
        string stopslog = "Stops: [";
        for (int i=0; i<21; i++)
        {
            startslog += starts[i].ToString() + (i==20?"]":", ");
            stopslog += stops[i].ToString() + (i == 20 ? "]" : ", ");
        }
        Debug.LogFormat("[_Play_ #{0}] {1}", ModuleId, startslog); 
        Debug.LogFormat("[_Play_ #{0}] {1}", ModuleId, stopslog);
    }

    void nextMove()
    {
        if (starts.Contains(move) || stops.Contains(move))
        {
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            move++;
            screen.text = move.ToString();
            Audio.PlaySoundAtTransform(fastSound.name, transform);
        }
    }

    void solve()
    {
        GetComponent<KMBombModule>().HandlePass();
        ModuleSolved = true;
        seedText.text = "Solved!";
        screen.text = "";
    }

    void press(int index)
    {
        if (move == (states[index] ? stops[index] : starts[index]))
        {
            if (states[index])
            {
                states[index] = false;
                buttonObjects[index].SetActive(false);
                if (index < 5)
                {
                    buttonObjects[4 * index + 1].SetActive(true);
                    buttonObjects[4 * index + 2].SetActive(true);
                    buttonObjects[4 * index + 3].SetActive(true);
                    buttonObjects[4 * index + 4].SetActive(true);
                }
                stops[index] = -1;
                Audio.PlaySoundAtTransform(clips[correct].name, transform);
                correct++;
                if (correct == 21) solve();
            }
            else
            {
                starts[index] = -1;
                startObjects[index].SetActive(false);
                stopObjects[index].SetActive(true);
                states[index] = true;
                Audio.PlaySoundAtTransform(startSound.name, transform);
            }
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }
    }


    void Awake()
    {
        screen.text = move.ToString();
        play.SetActive(true);
        ModuleId = ModuleIdCounter++;
        buttonObjects[0].SetActive(true);
        for (int i=1; i<21; i++) buttonObjects[i].SetActive(false);
        fastButton.OnInteract += delegate() { nextMove(); return true; } ;
        buttons[0].OnInteract = delegate () { press(0); return true; };
        buttons[1].OnInteract = delegate () { press(1); return true; };
        buttons[2].OnInteract = delegate () { press(2); return true; };
        buttons[3].OnInteract = delegate () { press(3); return true; };
        buttons[4].OnInteract = delegate () { press(4); return true; };
        buttons[5].OnInteract = delegate () { press(5); return true; };
        buttons[6].OnInteract = delegate () { press(6); return true; };
        buttons[7].OnInteract = delegate () { press(7); return true; };
        buttons[8].OnInteract = delegate () { press(8); return true; };
        buttons[9].OnInteract = delegate () { press(9); return true; };
        buttons[10].OnInteract = delegate () { press(10); return true; };
        buttons[11].OnInteract = delegate () { press(11); return true; };
        buttons[12].OnInteract = delegate () { press(12); return true; };
        buttons[13].OnInteract = delegate () { press(13); return true; };
        buttons[14].OnInteract = delegate () { press(14); return true; };
        buttons[15].OnInteract = delegate () { press(15); return true; };
        buttons[16].OnInteract = delegate () { press(16); return true; };
        buttons[17].OnInteract = delegate () { press(17); return true; };
        buttons[18].OnInteract = delegate () { press(18); return true; };
        buttons[19].OnInteract = delegate () { press(19); return true; };
        buttons[20].OnInteract = delegate () { press(20); return true; };
    }

    void Start()
    {
        generateSeed();
        generateSolution();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        if (!Command.RegexMatch("(([A-D][1-4]|>+)( |$))+"))
        {
            yield return "sendtochaterror Error";
        }
        else
        {
            var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string str in commandArgs)
            {
                if (str.RegexMatch("[A-D][1-4]"))
                {
                    int index = coords.IndexOf(i => i == str);
                    if (buttonObjects[5 + index].activeInHierarchy) buttons[5 + index].OnInteract();
                    else if (buttonObjects[1 + index/4].activeInHierarchy) buttons[1 + index/4].OnInteract();
                    else if (buttonObjects[0].activeInHierarchy) buttons[0].OnInteract();
                    else yield return "sendtochaterror Error2";
                }
                else
                {
                    for (int i=0; i<str.Length; i++)
                    {
                        fastButton.OnInteract();
                    }
                }
            }

        }
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!ModuleSolved)
        {
            if (starts.IndexOf(i => i == move) != -1) buttons[starts.IndexOf(i => i == move)].OnInteract();
            else if (stops.IndexOf(i => i == move) != -1) buttons[stops.IndexOf(i => i == move)].OnInteract();
            else fastButton.OnInteract();
            yield return new WaitForSeconds(0.15f);
        }
        yield return null;
    }
}
