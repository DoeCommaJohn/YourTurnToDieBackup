using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class UIMaster : MonoBehaviour
{

    int chapter = 0;
    int section = 0;
    readonly string[] splitter = { "\r\n" };
    readonly string myLoc = System.IO.Directory.GetCurrentDirectory();

    RawImage background;

    Transform DialogueUI;
    List<string[]> DialogueLines = new List<string[]>();
    float timeSinceLastLetter = 0;
    const float timeBetweenLetters = .01f;
    bool DialogueOverride = false;

    Transform PuzzleUI;
    List<string> Inventory = new List<string>();

    List<float> lstTextSizes;
    int remainingOptions = 0;

    Dictionary<string, int> dctTextVars = new Dictionary<string, int>();
    Dictionary<string, int> dctInteractedRooms = new Dictionary<string, int>();
    Dictionary<string, string[]> dctNeighbors = new Dictionary<string, string[]>();
    bool blnFreezeRooms = false;

    Transform PauseScreen;
    Transform SaveLoad;
    int topSave = 0;
    System.DateTime startTime;
    System.TimeSpan ellapsedTime;

    bool Loading = false;

    // Start is called before the first frame update

    void Start()
    {
        startTime = System.DateTime.Now;
        background = transform.GetChild(0).GetComponent<RawImage>();
        DialogueUI = transform.GetChild(1);
        PuzzleUI = transform.GetChild(2);
        PauseScreen = transform.GetChild(3);
        SaveLoad = transform.GetChild(4);
        PuzzleUI.GetChild(2).GetComponent<Button>().onClick.AddListener(MoveRooms);
        PuzzleUI.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { PauseScreen.gameObject.SetActive(true); blnFreezeRooms = true; Time.timeScale = 0; });
        PauseScreen.GetChild(2).GetChild(0).GetComponent<Button>().onClick.AddListener(delegate { ShowSaveScreen("Save to"); });
        PauseScreen.GetChild(2).GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { ShowSaveScreen("Load"); });
        for (int s = 2; s <= 6; s++)
        {
            int tempSave = s - 1;
            SaveLoad.GetChild(s).GetComponent<Button>().onClick.AddListener(delegate { AccessSave(tempSave); });
        }

        LoadFile();
    }

    private void AccessSave(int fileNumber)
    {
        Debug.Log("Accessing save");
        if (Loading)
            LoadGame(fileNumber);
        else
            SaveGame(fileNumber);
    }

    private void LoadGame(int fileNumber)
    {
        if (!System.IO.File.Exists(myLoc + "/Assets/Resources/SaveFiles/" + (fileNumber + topSave - 1).ToString() + ".txt"))
            return;
        string[] saveInfo = System.IO.File.ReadAllText(myLoc + "/Assets/Resources/SaveFiles/" + (fileNumber + topSave - 
            1).ToString() + ".txt").Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
        //string[] saveInfo = Resources.Load<TextAsset>("SaveFiles/" + (fileNumber + topSave - 1).ToString()).text.Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
        chapter = int.Parse(saveInfo[0].Substring(8));
        section = int.Parse(saveInfo[1].Substring(8)) - 1;
        startTime = System.DateTime.Now;
        saveInfo[2] = saveInfo[2].Substring(saveInfo[2].IndexOf(':') + 1);
        int hours = int.Parse(saveInfo[2].Substring(0, saveInfo[2].IndexOf(':')));
        saveInfo[2] = saveInfo[2].Substring(saveInfo[2].IndexOf(':') + 1);
        int mins = int.Parse(saveInfo[2].Substring(0, 2));
        int sec = int.Parse(saveInfo[2].Substring(3, 2));
        ellapsedTime = new System.TimeSpan(hours, mins, sec);

        //string[] tempSplit = { ";;;" };

        foreach (string itm in Inventory)
            DeleteInventory(itm);

        
        foreach (string invItem in saveInfo[3].Substring(10).Split(';'))
            if (invItem != "")
                Obtain(invItem);

        dctTextVars.Clear();
        foreach (string textVar in saveInfo[4].Substring(10).Split(';'))
            if (textVar.Contains(':'))
                dctTextVars.Add(textVar.Split(':')[0], int.Parse(textVar.Split(':')[1]));

        dctInteractedRooms.Clear();
        foreach (string visitedRoom in saveInfo[5].Substring(16).Split(';'))
            if (visitedRoom.Contains(':'))
                dctInteractedRooms.Add(visitedRoom.Split(':')[0], int.Parse(visitedRoom.Split(':')[1]));

        LoadFile();
        Move(saveInfo[6].Substring(9));

        Time.timeScale = 1;
        blnFreezeRooms = false;
    }

    private void SaveGame(int fileNumber)
    {
        Debug.Log("Saving Game");
        string saveWrite = "Chapter:" + chapter.ToString() + "\r\n";
        saveWrite += "Section:" + section.ToString() + "\r\n";
        ellapsedTime += System.DateTime.Now - startTime;
        startTime = System.DateTime.Now;
        //string DeleteMe = ellapsedTime.ToString(@"HH\:mm\:ss");
        saveWrite += "Time:" + ellapsedTime.TotalHours.ToString("00") + ":" +
            ellapsedTime.Minutes.ToString("00") + ":" + ellapsedTime.Seconds.ToString("00") + "\r\nInventory:";

        //Because saves can never happen during dialogue, DialogueLines will always be empty.
        //foreach (string[] diLine in DialogueLines)
        //    saveWrite += diLine[0] + ":::" + diLine[1] + ";;;";
        //saveWrite += "\r\nInventory:";
       
        foreach (string invItem in Inventory)
            saveWrite += invItem + ";";

        saveWrite += "\r\nVariables:";
        foreach (string varName in dctTextVars.Keys)
            saveWrite += varName + ':' + dctTextVars[varName].ToString() + ';';

        saveWrite += "\r\nInteractedRooms:";

        foreach (string visRoom in dctInteractedRooms.Keys)
            saveWrite += visRoom + ':' + dctInteractedRooms[visRoom] + ';';

        string currRoom = "";
        for (int room = 0; room < PuzzleUI.GetChild(5).childCount; room++)
            if (PuzzleUI.GetChild(5).GetChild(room).gameObject.activeInHierarchy)
            {
                currRoom = PuzzleUI.GetChild(5).GetChild(room).name;
                break;
            }
        saveWrite += "\r\nCurrRoom:" + currRoom + "\r\n";

        //if (!System.IO.File.Exists(myLoc + "/Resources/SaveFiles/" + (topSave + fileNumber - 1).ToString() + ".txt"))
        //    System.IO.File.Create(myLoc + "/Resources/SaveFiles/" + (topSave + fileNumber - 1).ToString() + ".txt");
        System.IO.File.WriteAllText(myLoc + "/Assets/Resources/SaveFiles/" + (topSave + fileNumber - 1).ToString() +
            ".txt", saveWrite);

        SaveLoad.gameObject.SetActive(false);
        //Current mode (dialogue/puzzle)
        //  Currently, this is guaranteed to be puzzle.
        //Current room

      
    }

    private void ShowSaveScreen(string SaveOrLoad)
    {
        Loading = SaveOrLoad == "Load";
        SaveLoad.GetChild(0).GetComponent<Text>().text = SaveOrLoad + " which file";
        for (int saveFile = 2; saveFile <= 6; saveFile++)
        {
            SaveLoad.GetChild(saveFile).GetChild(0).GetComponent<Text>().text = "File " + (saveFile - 1).ToString();
            if (System.IO.File.Exists(myLoc + "/Assets/Resources/SaveFiles/" + (saveFile - 1).ToString() + ".txt"))
            {
                string[] saveLines = System.IO.File.ReadAllText(myLoc + "/Assets/Resources/SaveFiles/" + (saveFile - 1).ToString() + 
                    ".txt").Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
                SaveLoad.GetChild(saveFile).GetChild(1).GetComponent<Text>().text = saveLines[0].Substring(8) + "-" + saveLines[1].Substring(8);
                SaveLoad.GetChild(saveFile).GetChild(2).GetComponent<Text>().text = saveLines[2].Substring(5);
            }
        }
        topSave = 1;        
        SaveLoad.gameObject.SetActive(true);
    }

    void MoveRooms()
    {
        for (int btn = 2; btn < PuzzleUI.GetChild(6).childCount; btn++)
            Destroy(PuzzleUI.GetChild(6).GetChild(btn).gameObject);
        if (PuzzleUI.GetChild(6).gameObject.activeInHierarchy)
        {
            PuzzleUI.GetChild(6).gameObject.SetActive(false);
            return;
        }


        blnFreezeRooms = true;
        string currentRoom = "";
        for (int room = 0; room < PuzzleUI.GetChild(5).childCount; room++)
            if (PuzzleUI.GetChild(5).GetChild(room).gameObject.activeInHierarchy)
            {
                currentRoom = PuzzleUI.GetChild(5).GetChild(room).name;
                break;
            }

        Vector2 minAnc = new Vector2(.1f, .7f);
        foreach (string adjacentRoom in dctNeighbors[currentRoom])
            if (adjacentRoom != "[None]")
            {
                GameObject btnRoom = Instantiate(PuzzleUI.GetChild(6).GetChild(1).gameObject, PuzzleUI.GetChild(6));
                RectTransform trns = btnRoom.GetComponent<RectTransform>();
                trns.anchorMin = minAnc;
                trns.anchorMax = new Vector2(minAnc.x + .2f, minAnc.y + .2f);
                if (Mathf.Approximately(minAnc.x, .7f))
                    minAnc = new Vector2(.1f, minAnc.y - .3f);
                else
                    minAnc = new Vector2(minAnc.x + .3f, minAnc.y);
                trns.sizeDelta = new Vector2(0, 0);
                trns.anchoredPosition = new Vector2(0, 0);

                trns.GetChild(0).GetComponent<Text>().text = adjacentRoom.Replace('_', ' ');
                btnRoom.GetComponent<Button>().onClick.AddListener(delegate { Move(adjacentRoom); blnFreezeRooms = false; PuzzleUI.GetChild(6).gameObject.SetActive(false); });
            }

        RectTransform cancelTransform = PuzzleUI.GetChild(6).GetChild(1).GetComponent<RectTransform>();
        cancelTransform.anchorMin = minAnc;
        cancelTransform.anchorMax = new Vector2(minAnc.x + .2f, minAnc.y + .2f);
        cancelTransform.sizeDelta = new Vector2(0, 0);
        cancelTransform.anchoredPosition = new Vector2(0, 0);

        PuzzleUI.GetChild(6).gameObject.SetActive(true);
    }

    void LoadFile()
    {
        for (int uie = 0; uie < transform.childCount; uie++)
            transform.GetChild(uie).gameObject.SetActive(false);

        section++;

        if (!System.IO.File.Exists(myLoc + "/Assets/Resources/GameFiles/Chapter" + chapter.ToString() + "/" + section.ToString() + ".txt"))
        {
            chapter++;
            section = 1;
        }
        string[] info = Resources.Load<TextAsset>("GameFiles/Chapter" + chapter.ToString() + "/" + section.ToString()).text.Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
        switch (info[0])
        {
            case "Dialogue":
                background.gameObject.SetActive(true);
                LoadDialogue(info.Skip(1).ToArray());
                break;
            case "PuzzleArea":
                LoadPuzzleRoom(info.Skip(1).ToArray());
                break;
        }
    }
    

    private void Obtain(string item)
    {
        if (!Inventory.Contains(item))
        {
            GameObject btnNew = Instantiate(PuzzleUI.GetChild(3).gameObject, PuzzleUI.GetChild(4));
            RectTransform trnsNew = btnNew.GetComponent<RectTransform>();
            trnsNew.anchorMin = new Vector2(Inventory.Count % 4 * .25f, .75f - (Inventory.Count / 4 * .25f));
            trnsNew.anchorMax = new Vector2((Inventory.Count % 4 + 1) * .25f, 1 - (Inventory.Count / 4 * .25f));
            trnsNew.sizeDelta = new Vector2(0, 0);
            trnsNew.anchoredPosition = new Vector2(0, 0);
            btnNew.GetComponent<Image>().sprite = Resources.Load<Sprite>("Items/" + item);
            btnNew.name = item;
            string[] ButtonDialogue = Resources.Load<TextAsset>("Items/" + item + "_Click").text.Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
            btnNew.GetComponent<Button>().onClick.AddListener(delegate { if (!DialogueUI.GetChild(3).GetChild(3).gameObject.activeInHierarchy) { LoadDialogue(ButtonDialogue); } });

            Inventory.Add(item);
        }
    }

    GameObject generateRawImage(string name, string imageFile, ref Texture2D ImageTexture)
    {
        GameObject imgBack = new GameObject(name);
        imgBack.AddComponent<RawImage>();
        RectTransform rt = imgBack.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(.15f, .3f);
        rt.anchorMax = new Vector2(.85f, 1);
        imgBack.transform.SetParent(PuzzleUI.GetChild(5));
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(0, 0);
        ImageTexture = Resources.Load<Texture2D>(imageFile);
        imgBack.GetComponent<RawImage>().texture = ImageTexture;
        return imgBack;
    }

    void LoadDialogue(string[] lines)
    {
        DialogueUI.gameObject.SetActive(true);
        blnFreezeRooms = true;
        DialogueLines.Clear();
        DialogueLines.Add(new string[] { "", "" });
        string currSpeaker = "";
        foreach (string dLine in lines)
            if (dLine.Length > 8 && dLine.Substring(0, 7) == "Speaker")
                currSpeaker = dLine.Substring(8);
            else if (dLine.Length > 0)
                DialogueLines.Add(new string[] { currSpeaker, dLine });
        DialogueOverride = true;
    }

    void LoadPuzzleRoom(string[] lines)
    {
        dctNeighbors.Clear();
        for (int uie = PuzzleUI.GetChild(5).childCount - 1; uie > 0; uie--)
            Destroy(PuzzleUI.GetChild(5).GetChild(uie).gameObject);

        background.gameObject.SetActive(false);
        DialogueUI.gameObject.SetActive(false);
        blnFreezeRooms = false;
        PuzzleUI.gameObject.SetActive(true);

        
        bool DebugMode = false;
        if (lines[0] == "DebugMode")
        {
            DebugMode = true;
            lines = lines.Skip(1).ToArray();
        }

        int con = 0;
        while (con < lines.Length)
        {
            Texture2D txtBack = null;
            GameObject img = generateRawImage(lines[con], "Backgrounds/" + lines[con], ref txtBack);
            img.SetActive(con == 0);
            con += 1;
            while (con < lines.Length && (lines[con].Length < 9 || lines[con].Substring(0, 9) != "Neighbors"))
            {
                RectTransform btnNew = Instantiate(PuzzleUI.GetChild(3).gameObject, img.transform).GetComponent<RectTransform>();
                float[] coords = System.Array.ConvertAll(lines[con].Split(';')[0].Split('x'), float.Parse); //This must be a float in order to prevent integer division below.
                btnNew.anchorMin = new Vector2(coords[0] / txtBack.width, coords[1] / txtBack.height);
                btnNew.anchorMax = new Vector2(coords[2] / txtBack.width, coords[3] / txtBack.height);
                btnNew.sizeDelta = new Vector2(0, 0);
                btnNew.anchoredPosition = new Vector2(0, 0);
                //During debugmode, show pink rectangles over each button, but these should be hidden during the actual game.
                if (DebugMode)
                    btnNew.GetComponent<Image>().color = new Color(1, 0, 1, 1);
                else
                    btnNew.GetComponent<Image>().color = new Color(1, 0, 1, 0);
                int tempCon = con;
                btnNew.GetComponent<Button>().onClick.AddListener(delegate { ClickObject(lines[tempCon].Split(';')[1]); });
                con++;
                string DeleteMe = lines[con].Substring(0, 9);
            }
            dctNeighbors.Add(img.name, lines[con].Substring(10).Split(';'));

            con++;
        }
    }

    private void ClickObject(string obj)
    {
        if (blnFreezeRooms)
            return;
        //JohnComeHere. This is a pretty inefficient method of doing this.
        //The general purpose of this code is to increment the text file in order to prevent repeats of events.
        //  For example, if the player opens the room and finds a gun, they shouldn't find the same gun again.
        //  The first time, ChapterN/Room1_1.txt would be opened, but once that has been opened, [Read] will be added to the top of the text file,
        //      and ChapterN/Room1_2.txt will be opened instead.


        if (!dctInteractedRooms.ContainsKey(obj))
            dctInteractedRooms.Add(obj, 0);

        if (System.IO.File.Exists(myLoc + "/Assets/Resources/GameFiles/" + obj + "_" + (dctInteractedRooms[obj] + 1).ToString() + ".txt"))
            dctInteractedRooms[obj] += 1;

        LoadDialogue(Resources.Load<TextAsset>("GameFiles/" + obj + "_" + dctInteractedRooms[obj]).text.Split(splitter, System.StringSplitOptions.RemoveEmptyEntries));
    }



    // Update is called once per frame
    void FixedUpdate()
    {
        if (DialogueLines.Count > 0)
            if (!DialogueOverride && DialogueLines[0][1].Length > 0)
                if (Input.GetKeyUp(KeyCode.Return) || Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space))
                {
                    DialogueUI.GetChild(3).GetChild(1).GetComponent<Text>().text += DialogueLines[0][1];
                    DialogueLines[0][1] = "";
                    timeSinceLastLetter = 0;
                }
                else if (timeSinceLastLetter > timeBetweenLetters)
                {
                    DialogueUI.GetChild(3).GetChild(1).GetComponent<Text>().text += DialogueLines[0][1].Substring(0, 1);
                    DialogueLines[0][1] = DialogueLines[0][1].Substring(1);
                    timeSinceLastLetter = 0;
                }
                else
                    timeSinceLastLetter += Time.deltaTime;
            else if (DialogueOverride || (!DialogueUI.GetChild(3).GetChild(3).gameObject.activeInHierarchy && (Input.GetKeyUp(KeyCode.Return) || Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space))))
            {
                if (DialogueOverride)
                    DialogueOverride = false;
                else if (DialogueLines[0][1].Length > 0)
                {
                    DialogueUI.GetChild(3).GetChild(2).GetChild(1).GetComponent<Text>().text += DialogueLines[0][1];
                    DialogueLines[0][1] = "";
                    return;
                }
                if (DialogueUI.GetChild(3).GetChild(3).gameObject.activeInHierarchy)
                {
                    for (int opt = DialogueUI.GetChild(3).GetChild(3).childCount - 1; opt > 1; opt--)
                        Destroy(DialogueUI.GetChild(3).GetChild(3).GetChild(opt).gameObject);
                    DialogueUI.GetChild(3).GetChild(3).gameObject.SetActive(false);
                }
                DialogueLines.RemoveAt(0);
                DialogueUI.GetChild(3).GetChild(1).GetComponent<Text>().text = "";

                if (DialogueLines.Count > 0)
                {
                    //blnDialogueApproved = true;
                    if (DialogueLines[0][1].Length > 11 && DialogueLines[0][1].Substring(0, 10) == "Background")
                    {
                        background.texture = Resources.Load<Texture2D>("Backgrounds/" + DialogueLines[0][1].Substring(11));
                        //DialogueLines.RemoveAt(0);
                        //blnDialogueApproved = false;
                        DialogueOverride = true;
                    }
                    //Slash can be used to access functions from a text file. For example "/obtain revolver" within a text file will call the obtain function and send it a revolver.
                    else if (DialogueLines[0][1][0] == '/')
                    {
                        string command = DialogueLines[0][1];
                        if (command.Contains(' '))
                            command = command.Substring(0, command.IndexOf(' '));
                        switch (command){
                            case "/Obtain":
                                Obtain(DialogueLines[0][1].Substring(8));
                                break;
                            case "/Destroy":
                                DeleteInventory(DialogueLines[0][1].Substring(9));
                                break;
                            case "/Write":
                                TextWrite(DialogueLines[0][1].Substring(7));
                                break;
                            case "/Read":
                                TextRead(DialogueLines[0][1].Substring(6));
                                break;
                            case "/Move":
                                Move(DialogueLines[0][1].Substring(6));
                                break;
                            case "/Clear":
                                ClearVar(DialogueLines[0][1].Substring(7));
                                break;
                            case "/EndPuzzle":
                                LoadFile();
                                break;
                        }
                        DialogueOverride = true;
                        //DialogueLines.RemoveAt(0);
                        //blnDialogueApproved = false;
                    }
                    else if (DialogueLines[0][0].Contains("[None]"))
                    {
                        //Hide all the speakers
                        DialogueUI.GetChild(0).gameObject.SetActive(false);
                        DialogueUI.GetChild(1).gameObject.SetActive(false);
                        DialogueUI.GetChild(2).gameObject.SetActive(false);
                        if (DialogueLines[0][0].Contains('_'))
                            DialogueUI.GetChild(3).GetChild(2).GetChild(1).GetComponent<Text>().text = DialogueLines[0][0].Substring(0, DialogueLines[0][0].IndexOf('_'));
                        else
                            DialogueUI.GetChild(3).GetChild(2).GetChild(1).GetComponent<Text>().text = "";
                    }
                    else if (DialogueLines[0][0].Contains('&'))
                    {
                        DialogueUI.GetChild(3).GetChild(2).GetChild(1).GetComponent<Text>().text = DialogueLines[0][0].Substring(0, DialogueLines[0][0].IndexOf('_'));
                        DialogueLines[0][0] = DialogueLines[0][0].Substring(DialogueLines[0][0].IndexOf('_') + 1);
                        DialogueUI.GetChild(0).gameObject.SetActive(false);
                        DialogueUI.GetChild(1).GetComponent<RawImage>().texture = Resources.Load<Texture2D>("CharacterSprites/" + DialogueLines[0][0].Substring(0, DialogueLines[0][0].IndexOf('&')));
                        DialogueUI.GetChild(1).gameObject.SetActive(true);
                        DialogueUI.GetChild(2).GetComponent<RawImage>().texture = Resources.Load<Texture2D>("CharacterSprites/" + DialogueLines[0][0].Substring(DialogueLines[0][0].IndexOf('&') + 1));
                        DialogueUI.GetChild(2).gameObject.SetActive(true);
                    }
                    else
                    {
                        DialogueUI.GetChild(3).GetChild(2).GetChild(1).GetComponent<Text>().text = DialogueLines[0][0].Substring(0, DialogueLines[0][0].IndexOf('/'));

                        DialogueUI.GetChild(0).GetComponent<RawImage>().texture = Resources.Load<Texture2D>("CharacterSprites/" + DialogueLines[0][0]);
                        DialogueUI.GetChild(0).gameObject.SetActive(true);
                        DialogueUI.GetChild(1).gameObject.SetActive(false);
                        DialogueUI.GetChild(2).gameObject.SetActive(false);
                    }

                    if (DialogueLines[0][1].Contains('|'))
                    {
                        for (int prevOpt = 1; prevOpt < DialogueUI.GetChild(3).GetChild(3).childCount; prevOpt++)
                            Destroy(DialogueUI.GetChild(3).GetChild(3).GetChild(prevOpt).gameObject);
                        lstTextSizes = new List<float>();
                        
                        string[] allOptions = DialogueLines[0][1].Split('|');
                        DialogueUI.GetChild(3).GetChild(3).gameObject.SetActive(true);
                        DialogueUI.GetChild(3).GetChild(3).GetComponent<RectTransform>().anchorMax = new Vector2(1, 1 + (.325f*allOptions.Length));
                        remainingOptions = allOptions.Length;
                        float inc = .2f;
                        for (int opt = 0; opt < allOptions.Length; opt++)
                        {
                            GameObject btnOption = Instantiate(DialogueUI.GetChild(3).GetChild(3).GetChild(0).gameObject, DialogueUI.GetChild(3).GetChild(3));
                            btnOption.GetComponent<RectTransform>().anchorMin = new Vector2(.5f, inc);
                            inc += ((float)1 / allOptions.Length) * .6f * .8f;
                            btnOption.GetComponent<RectTransform>().anchorMax = new Vector2(.5f, inc);
                            inc += ((float)1 / allOptions.Length) * .2f;

                            btnOption.transform.GetChild(0).GetComponent<Text>().text = allOptions[opt];

                            btnOption.SetActive(true);
                            int choice = opt;
                            btnOption.GetComponent<Button>().onClick.AddListener(delegate { ChooseOption(choice + 1); });
                        }
                        //DialogueOverride = true;
                        DialogueLines.RemoveAt(0);
                    }
                }
                else
                {
                    DialogueUI.gameObject.SetActive(false);
                    blnFreezeRooms = false;
                    //LoadFile();
                }       
            }
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
            if (SaveLoad.gameObject.activeInHierarchy)
                SaveLoad.gameObject.SetActive(false);
            else if (PauseScreen.gameObject.activeInHierarchy)
            {
                Time.timeScale = 1;
                blnFreezeRooms = false;
                PauseScreen.gameObject.SetActive(false);
            }

        if (SaveLoad.gameObject.activeInHierarchy)
            if (topSave < 45 && (Input.GetKeyUp(KeyCode.DownArrow) || Input.mouseScrollDelta.y < 0))
            {
                topSave += 1;
                for (int b = 2; b <= 5; b++)
                    for (int c = 0; c < 3; c++)
                        SaveLoad.GetChild(b).GetChild(c).GetComponent<Text>().text = SaveLoad.GetChild(b + 1).GetChild(c).GetComponent<Text>().text;

                SaveLoad.GetChild(6).GetChild(0).GetComponent<Text>().text = "File " + (topSave + 4).ToString();
                if (System.IO.File.Exists(myLoc + "/Assets/Resources/SaveFiles/" + (topSave + 4).ToString() + ".txt"))
                {
                    string[] saveLines = System.IO.File.ReadAllText(myLoc + "/Assets/Resources/SaveFiles/" + (topSave + 
                        4).ToString() + ".txt").Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
                    //string[] saveLines = Resources.Load<TextAsset>("SaveFiles/" + (topSave + 4).ToString()).text.Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
                    SaveLoad.GetChild(6).GetChild(1).GetComponent<Text>().text = saveLines[0].Substring(8) + "-" + saveLines[1].Substring(8);
                    SaveLoad.GetChild(6).GetChild(2).GetComponent<Text>().text = saveLines[2].Substring(5);
                }
                else
                {
                    SaveLoad.GetChild(6).GetChild(1).GetComponent<Text>().text = "";
                    SaveLoad.GetChild(6).GetChild(2).GetComponent<Text>().text = "";
                }
            }
            else if (topSave > 1 && (Input.GetKeyUp(KeyCode.UpArrow) || Input.mouseScrollDelta.y > 0))
            {
                topSave -= 1;
                for (int b = 6; b >= 3; b--)
                    for (int c = 0; c < 3; c++)
                        SaveLoad.GetChild(b).GetChild(c).GetComponent<Text>().text = SaveLoad.GetChild(b - 1).GetChild(c).GetComponent<Text>().text;

                SaveLoad.GetChild(2).GetChild(0).GetComponent<Text>().text = "File " + topSave.ToString();
                if (System.IO.File.Exists(myLoc + "/Assets/Resources/SaveFiles/" + topSave.ToString() + ".txt"))
                {
                    string[] saveLines = System.IO.File.ReadAllText(myLoc + "/Assets/Resources/SaveFiles/" + 
                        topSave.ToString() + ".txt").Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
                    SaveLoad.GetChild(2).GetChild(1).GetComponent<Text>().text = saveLines[0].Substring(8) + "-" + saveLines[1].Substring(8);
                    SaveLoad.GetChild(2).GetChild(2).GetComponent<Text>().text = saveLines[2].Substring(5);
                }
                else
                {
                    SaveLoad.GetChild(2).GetChild(1).GetComponent<Text>().text = "";
                    SaveLoad.GetChild(2).GetChild(2).GetComponent<Text>().text = "";
                }
            }


            }

    private void ClearVar(string varName)
    {
        if (dctTextVars.ContainsKey(varName))
            dctTextVars.Remove(varName);
    }
    
    private void Move(string NewLocation)
    {
        for (int loc = 0; loc < PuzzleUI.GetChild(5).childCount; loc++)
            PuzzleUI.GetChild(5).GetChild(loc).gameObject.SetActive(PuzzleUI.GetChild(5).GetChild(loc).name == NewLocation);
    }

    private void TextWrite(string varInfo)
    {
        string varName = "";
        int val = 1;
        if (varInfo.Contains(' '))
        {
            varName = varInfo.Substring(0, varInfo.IndexOf(' '));
            val = int.Parse(varInfo.Substring(varInfo.IndexOf(' ') + 1));
        }
        else
            varName = varInfo;



        if (dctTextVars.ContainsKey(varName))
            dctTextVars[varName] = val;
        else
            dctTextVars.Add(varName, val);
    }

    private void TextRead(string varInfo)
    {
        if (dctTextVars.ContainsKey(varInfo))
            ChooseOption(dctTextVars[varInfo]);
        else
            ChooseOption(0);
    }

    private void ChooseOption(int choiceIndex)
    {
        int con = 0;
        while (con < DialogueLines.Count)
        {
            string diLine = DialogueLines[con][1];
            if (diLine == "ENDSPLIT")
            {
                DialogueLines.RemoveAt(con);
                break;
            }
            else if (diLine.Length > 2 && diLine[1] == ')')
                if (diLine.Substring(0, 1) == choiceIndex.ToString())
                    DialogueLines[con][1] = diLine.Substring(2);
                else
                {
                    DialogueLines.RemoveAt(con);
                    con--;
                }
            con++;
        }
        DialogueOverride = true;
    }

    private void DeleteInventory(string destroyItem)
    {
        Inventory.Remove(destroyItem);
        bool itmDeleted = false;
        for (int itm = 0; itm < PuzzleUI.GetChild(4).childCount; itm++)
            //If the item has been deleted, all items after it should be moved up.
            if (itmDeleted)
            {
                RectTransform rt = PuzzleUI.GetChild(4).GetChild(itm).GetComponent<RectTransform>();
                if (rt.anchorMin.x != 0)
                    rt.anchorMin = new Vector2(rt.anchorMin.x - .25f, rt.anchorMin.y);
                else
                    rt.anchorMin = new Vector2(.75f, rt.anchorMax.y - .25f);
                rt.anchorMax = new Vector2(rt.anchorMin.x + .25f, rt.anchorMin.y + .25f);
            }
            else if (PuzzleUI.GetChild(4).GetChild(itm).name == destroyItem)
            {
                Destroy(PuzzleUI.GetChild(4).GetChild(itm).gameObject);
                itmDeleted = true;
            }
    }

    public void SetTextWidth(float width)
    {
        if (remainingOptions > 0)
        {
            lstTextSizes.Add(width);
            remainingOptions--;
            if (remainingOptions == 0)
            {
                lstTextSizes.Sort();
                float maxWidth = lstTextSizes[lstTextSizes.Count - 1];
                Transform optOutline = DialogueUI.GetChild(3).GetChild(3);
                optOutline.GetComponent<RectTransform>().sizeDelta = new Vector2(maxWidth * 1.5f, 0);
                optOutline.GetComponent<RectTransform>().anchoredPosition = new Vector2(-maxWidth*3/4, 0);
                for (int opt = 0; opt < optOutline.childCount; opt++)
                    optOutline.GetChild(opt).GetComponent<RectTransform>().sizeDelta = new Vector2(maxWidth * 1.25f, 0);
                
            }
        }
    }
}
