using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class playScript : MonoBehaviour
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
        switch (str)
        {
            case "00000": return new int[4] { 0, 1, 2, 3 };
            case "01000": return new int[4] { 1, 0, 2, 3 };
            case "10000": return new int[4] { 2, 0, 1, 3 };
            case "11000": return new int[4] { 3, 0, 1, 2 };
            case "00001": return new int[4] { 0, 1, 3, 2 };
            case "01001": return new int[4] { 1, 0, 3, 2 };
            case "10001": return new int[4] { 2, 0, 3, 1 };
            case "11001": return new int[4] { 3, 0, 2, 1 };
            case "00010": return new int[4] { 0, 2, 1, 3 };
            case "01010": return new int[4] { 1, 2, 0, 3 };
            case "10010": return new int[4] { 2, 1, 0, 3 };
            case "11010": return new int[4] { 3, 1, 0, 2 };
            case "00011": return new int[4] { 0, 2, 3, 1 };
            case "01011": return new int[4] { 1, 2, 3, 0 };
            case "10011": return new int[4] { 2, 1, 3, 0 };
            case "11011": return new int[4] { 3, 1, 2, 0 };
            case "00100": return new int[4] { 0, 3, 1, 2 };
            case "01100": return new int[4] { 1, 3, 0, 2 };
            case "10100": return new int[4] { 2, 3, 0, 1 };
            case "11100": return new int[4] { 3, 2, 0, 1 };
            case "00101": return new int[4] { 0, 3, 2, 1 };
            case "01101": return new int[4] { 1, 3, 2, 0 };
            case "10101": return new int[4] { 2, 3, 1, 0 };
            case "11101": return new int[4] { 3, 2, 1, 0 };
            case "00110": return new int[4] { 0, 1, 2, 3 };
            case "01110": return new int[4] { 1, 0, 2, 3 };
            case "10110": return new int[4] { 2, 0, 1, 3 };
            case "11110": return new int[4] { 3, 0, 1, 2 };
            case "00111": return new int[4] { 0, 1, 3, 2 };
            case "01111": return new int[4] { 1, 0, 3, 2 };
            case "10111": return new int[4] { 2, 0, 3, 1 };
            case "11111": return new int[4] { 3, 0, 2, 1 };
            default: return new int[4] { 0,1,2,3 };
        }
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
            digit = digit == 47 ? 18 : digit;
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
        int[] order = toOrder(binarySeed.Substring(2, 5));
        for (int i=0; i<4; i++)
        {
            starts[1 + i] = stops[0] + order[i] + 1;
            stops[1 + i] = starts[1 + i] + toBase10(binarySeed.Substring(7 + 2 * i, 2)) + 1;
        }
        for (int j=0; j<4; j++)
        {
            order = toOrder(binarySeed.Substring(15+5*j, 5));
            //11ORDER11223344ORDERORDERORDERORDER11223344112233441122334411223344
            //0         1         2         3         4         5         6     6
            for (int i = 0; i < 4; i++)
            {
                starts[5 + 4 * j + i] = stops[1 + j] + order[i] + 1;
                stops[5 + 4 * j + i] = starts[5 + 4 * j + i] + toBase10(binarySeed.Substring(35+8*j+2*i, 2)) + 1;
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
        Debug.LogFormat("[_Play_ #{0}] Pressed {1}", ModuleId, index);
        if (move == (states[index] ? stops[index] : starts[index]))
        {
            if (states[index])
            {
                states[index] = false;
                buttonObjects[index].SetActive(false);
                buttonObjects[index].transform.localPosition -= new Vector3(0, 2);
                if (index < 5)
                {
                    buttonObjects[4 * index + 1].transform.localPosition += new Vector3(0, 2);
                    buttonObjects[4 * index + 2].transform.localPosition += new Vector3(0, 2);
                    buttonObjects[4 * index + 3].transform.localPosition += new Vector3(0, 2);
                    buttonObjects[4 * index + 4].transform.localPosition += new Vector3(0, 2);

                    switch (index)
                    {
                        case 0:
                            {

                                buttons[1].OnInteract += delegate () { press(1); return false; };
                                buttons[2].OnInteract += delegate () { press(2); return false; };
                                buttons[3].OnInteract += delegate () { press(3); return false; };
                                buttons[4].OnInteract += delegate () { press(4); return false; };
                                break;
                            }
                        case 1:
                            {
                                buttons[5].OnInteract += delegate () { press(5); return false; };
                                buttons[6].OnInteract += delegate () { press(6); return false; };
                                buttons[7].OnInteract += delegate () { press(7); return false; };
                                buttons[8].OnInteract += delegate () { press(8); return false; };
                                break;
                            }
                        case 2:
                            {
                                buttons[ 9].OnInteract += delegate () { press( 9); return false; };
                                buttons[10].OnInteract += delegate () { press(10); return false; };
                                buttons[11].OnInteract += delegate () { press(11); return false; };
                                buttons[12].OnInteract += delegate () { press(12); return false; };
                                break;
                            }
                        case 3:
                            {
                                buttons[13].OnInteract += delegate () { press(13); return false; };
                                buttons[14].OnInteract += delegate () { press(14); return false; };
                                buttons[15].OnInteract += delegate () { press(15); return false; };
                                buttons[16].OnInteract += delegate () { press(16); return false; };
                                break;
                            }
                        case 4:
                            {
                                buttons[17].OnInteract += delegate () { press(17); return false; };
                                buttons[18].OnInteract += delegate () { press(18); return false; };
                                buttons[19].OnInteract += delegate () { press(19); return false; };
                                buttons[20].OnInteract += delegate () { press(20); return false; };
                                break;
                            }
                    }
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
        //buttonObjects[0].SetActive(true);
        for (int i=0; i<21; i++) buttonObjects[i].SetActive(true);
        for (int i = 1; i < 21; i++) buttonObjects[i].transform.localPosition -= new Vector3(0, 2);
        fastButton.OnInteract += delegate() { nextMove(); return false; } ;
        buttons[0].OnInteract += delegate () { press(0); return false; };
        
    }

    void Start()
    {
        generateSeed();
        generateSolution();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} <coordinate> to press the button on that coordinate. Use !{0} > to press fast-forward button. Example: !{0} A1 >>> A2 > A3 B1 C2 > D3";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
        if (!Command.RegexMatch("(([A-Da-d][1-4]|>+)( |$))+"))
        {
            yield return "sendtochaterror Сommand is not valid.";
        }
        else
        {
            var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string str in commandArgs)
            {
                if (str.RegexMatch("[A-D][1-4]"))
                {
                    int index = coords.IndexOf(i => i == str);
                    Debug.Log(index.ToString());
                    if (buttonObjects[0].activeInHierarchy) buttons[0].OnInteract();
                    else if (buttonObjects[1 + index/4].activeInHierarchy) buttons[1 + index/4].OnInteract();
                    else if (buttonObjects[5 + index].activeInHierarchy) buttons[5 + index].OnInteract();
                    else yield return "sendtochaterror No such button.";
                }
                else
                {
                    for (int i=0; i<str.Length; i++)
                    {
                        fastButton.OnInteract();
                    }
                }
                yield return new WaitForSeconds(0.15f);
            }

        }
        
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
