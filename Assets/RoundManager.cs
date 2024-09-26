using Demo.Scripts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows;

[System.Serializable]
public struct RoundConfigs
{
    public List<float> roundFPS;
    public List<float> spikeMagnitude;
    public List<bool> onAimSpikeEnabled;
    public List<bool> onReloadSpikeEnabled;
    public List<bool> onMouseSpikeEnabled;
    public List<bool> onEnemySpawnSpikeEnabled;
}

[System.Serializable]
public struct PlayerTickLog
{
    public List<string> time;
    public List<float> roundTimer;
    public List<float> mouseX;
    public List<float> mouseY;

    public List<float> playerX;
    public List<float> playerY;
    public List<float> playerZ;

    public List<Quaternion> playerRot;

    public List<Vector3> enemyPos;
    public List<bool> isADS;

    public List<float> scorePerSec;
}

public class RoundManager : MonoBehaviour
{
    public RoundConfigs roundConfigs;

    public float roundDuration;

    public float roundTimer;

    public int totalRoundNumber;

    public List<int> indexArray = new List<int>();

    public GameManager gameManager;

    public int currentRoundNumber;

    public String sessionStartTime;

    public FPSController playerController;

    public long roundFrameCount = 0;
    public double frametimeCumulativeRound = 0;

    public string fileNameSuffix = "";
    public String filenamePerTick = "Data\\ClientDataPerTick.csv";
    public String filenamePerRound = "Data\\ClientDataPerRound.csv";

    public float qoeValue;

    public int sessionID = -1;

    public bool isFTStudy;

    // Start is called before the first frame update
    void Start()
    {
        currentRoundNumber = 1;
        fileNameSuffix = GenRandomID(6).ToString();
        sessionStartTime = System.DateTime.Now.ToString("yy:mm:dd:hh:mm:ss");

        gameManager = GetComponent<GameManager>();

        ReadGlobalConfig();

        ReadFromLatinSquare();

        totalRoundNumber = roundConfigs.roundFPS.Count;

        for (int i = 0; i < totalRoundNumber; i++)
        {
            indexArray.Add(i);
        }

        // Shuffle the list
        //Shuffle(indexArray);

        //LEGACY CODE
        //Add practice round
        /*int temp = indexArray[totalRoundNumber - 1];
        indexArray[totalRoundNumber - 1] = indexArray[0];
        indexArray[0] = 0;
        indexArray.Add(temp);
        indexArray.Add(0);
        totalRoundNumber++;*/


        playerController.isQoeDisabled = true;
        SetRounConfig();
    }
    void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (roundTimer > 0 && playerController.isPlayerReady && playerController.isQoeDisabled)
        {
            roundTimer -= Time.deltaTime;
            frametimeCumulativeRound += Time.deltaTime;
            roundFrameCount++;
        }
        else if (roundTimer <= 0 && playerController.isQoeDisabled)
        {
            playerController.isQoeDisabled = false;
            playerController.ResetPlayerAndDestroyEnemy();
        }
    }


    void ReadGlobalConfig()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\GlobalConfig.csv");
        bool EOF = false;
        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var dataValues = line.Split(',');
                roundDuration = float.Parse(dataValues[0]);
                isFTStudy = bool.Parse(dataValues[1]);
                playerController.aimSpikeDelay = float.Parse(dataValues[2]);
                playerController.mouseSpikeDelay = float.Parse(dataValues[3]);
                playerController.mouseSpikeDegreeOfMovement = float.Parse(dataValues[4]);
                playerController.enemySpeedGlobal = float.Parse(dataValues[5]);
                playerController.enemyHealthGlobal = float.Parse(dataValues[6]);
                playerController.reticleSizeMultiplier = float.Parse(dataValues[7]);

                playerController.onHitScore = int.Parse(dataValues[8]);
                playerController.onMissScore = int.Parse(dataValues[9]);
                playerController.onKillScore = int.Parse(dataValues[10]);
                playerController.onDeathScore = int.Parse(dataValues[11]);
            }
        }
    }

    public void ReadFromLatinSquare()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\SessionID.csv");
        bool EOF = false;
        roundConfigs.roundFPS.Clear();

        sessionID = -1;

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var dataValues = line.Split(',');
                sessionID = int.Parse(dataValues[0]);
            }
        }



        line = null;
        strReader = new StreamReader("Data\\Configs\\LatinSquare.csv");
        EOF = false;
        int index = 1;

        if (!isFTStudy)
        {
            //Practice
            roundConfigs.roundFPS.Add(500);
            roundConfigs.roundFPS.Add(7);

            while (!EOF)
            {
                line = strReader.ReadLine();

                if (line == null)
                {
                    EOF = true;
                    break;
                }
                else
                {
                    var dataValues = line.Split(',');
                    if (index == sessionID)
                    {
                        for (int i = 0; i < dataValues.Length; i++)
                            roundConfigs.roundFPS.Add(float.Parse(dataValues[i]));

                        /*// Round x2
                        for (int i = 0; i < dataValues.Length; i++)
                            roundConfigs.roundFPS.Add(float.Parse(dataValues[i]));*/
                        FrameRateSudySpikeConfigFiller(dataValues.Length);
                        break;
                    }
                }
                index++;
            }
        }
        else
        {
            //Practice round single for FT study
            roundConfigs.roundFPS.Add(500);
            roundConfigs.spikeMagnitude.Add(0);
            roundConfigs.onAimSpikeEnabled.Add(false);
            roundConfigs.onReloadSpikeEnabled.Add(false);
            roundConfigs.onMouseSpikeEnabled.Add(false);
            roundConfigs.onEnemySpawnSpikeEnabled.Add(false);

            ReadFTStudyCSV();
        }
    }

    public void FrameRateSudySpikeConfigFiller(int length)
    {
        for (int i = 0; i < length * 2 + 2; i++)
        {
            roundConfigs.spikeMagnitude.Add(100);
            roundConfigs.onAimSpikeEnabled.Add(false);
            roundConfigs.onReloadSpikeEnabled.Add(false);
            roundConfigs.onMouseSpikeEnabled.Add(false);
            roundConfigs.onEnemySpawnSpikeEnabled.Add(false);
        }
    }


    // Primary Config
    public void ReadFTStudyCSV()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\LatinMap.csv");
        bool EOF = false;
        List<string> latinMap = new List<string>();

        while (!EOF)
        {

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                latinMap.Add(strReader.ReadLine());
            }
        }

        line = null;
        strReader = new StreamReader("Data\\Configs\\SessionID.csv");
        EOF = false;

        sessionID = -1;

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var dataValues = line.Split(',');
                sessionID = int.Parse(dataValues[0]);
            }
        }


        line = null;
        strReader = new StreamReader("Data\\Configs\\RoundConfig.csv");
        EOF = false;
        roundConfigs.roundFPS.Clear();

        int index = 1;

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var configVals = line.Split(',');

                if (index == sessionID)

                {
                    for (int i = 0; i < configVals.Length; i++)
                    {
                        string config = latinMap[Int32.Parse(configVals[i]) - 1];
                        var dataValues = config.Split(',');

                        Debug.Log(dataValues[1]);
                        roundConfigs.roundFPS.Add(float.Parse(dataValues[0]));
                        roundConfigs.spikeMagnitude.Add(float.Parse(dataValues[1]));
                        roundConfigs.onAimSpikeEnabled.Add(bool.Parse(dataValues[2]));
                        roundConfigs.onReloadSpikeEnabled.Add(bool.Parse(dataValues[3]));
                        roundConfigs.onMouseSpikeEnabled.Add(bool.Parse(dataValues[4]));
                        roundConfigs.onEnemySpawnSpikeEnabled.Add(bool.Parse(dataValues[5]));
                    }
                }
            }
            index++;
        }
    }

    String GenRandomID(int len)
    {
        String alphanum = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        String tmp_s = "";

        for (int i = 0; i < len; ++i)
        {
            tmp_s += alphanum[UnityEngine.Random.Range(0, 1000) % (alphanum.Length - 1)];
        }

        return tmp_s;
    }

    public void SetRounConfig()
    {
        roundTimer = roundDuration;
        gameManager.isFixedFT = false;


        Application.targetFrameRate = (int)roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]];


        playerController.isAimSpikeEnabled = roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]];
        playerController.isReloadSpikeEnabled = roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]];
        playerController.isMouseMovementSpikeEnabled = roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]];
        playerController.isEnemySpawnSpikeEnabled = roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]];

        gameManager.delayDuration = roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]];

        roundFrameCount = 0;
        frametimeCumulativeRound = 0;

    }

    public void LogRoundData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        TextWriter textWriter = null;
        filenamePerRound = "Data\\Logs\\RoundData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";

        while (textWriter == null)
            textWriter = System.IO.File.AppendText(filenamePerRound);

        float accuracy = 0;
        if (playerController.shotsFiredPerRound > 0)
        {
            accuracy = (float)playerController.shotsHitPerRound / (float)playerController.shotsFiredPerRound;
        }

        float degXTargetAvg = (float)playerController.degreeToTargetXCumulative / (float)playerController.roundKills;
        float degXShootAvg = (float)playerController.degreeToShootXCumulative / (float)playerController.roundKills;
        float enemySizeOnSpawnAvg = (float)playerController.enemySizeCumulative / (float)playerController.roundKills;

        float timeToTargetAvg = (float)playerController.timeToTargetEnemyCumulative / (float)playerController.roundKills;
        float timeToHitAvg = (float)playerController.timeToHitEnemyCumulative / (float)playerController.roundKills;
        float timeToKillAvg = (float)playerController.timeToKillEnemyCumulative / (float)playerController.roundKills;

        double avgFT = frametimeCumulativeRound / roundFrameCount;
        double avgFPS = 1 / avgFT;

        String roundLogLine =
           sessionID.ToString() + "," +
           currentRoundNumber.ToString() + "," +
           sessionStartTime.ToString() + "," +
           System.DateTime.Now.ToString() + "," +
           roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]].ToString() + "," +
           roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]].ToString() + "," +
           roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
           roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
           roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
           roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
           indexArray[currentRoundNumber - 1].ToString() + "," +
           playerController.score + "," +
           playerController.shotsFiredPerRound + "," +
           playerController.shotsHitPerRound + "," +
           playerController.headshotsHitPerRound + "," +
           playerController.realoadCountPerRound + "," +
           playerController.tacticalReloadCountPerRound + "," +
           accuracy.ToString() + "," +
           playerController.roundKills + "," +
           playerController.roundDeaths + "," +
           playerController.distanceTravelledPerRound + "," +
           playerController.delXCumilative.ToString() + "," +
           playerController.delYCumilative.ToString() + "," +
           (playerController.delXCumilative + playerController.delYCumilative).ToString() + "," +
           frametimeCumulativeRound.ToString() + "," +
           roundFrameCount.ToString() + "," +
           avgFT.ToString() + "," +
           avgFPS.ToString() + "," +
           playerController.perRoundAimSpikeCount.ToString() + "," +
           playerController.perRoundReloadSpikeCount.ToString() + "," +
           playerController.perRoundMouseMovementSpikeCount.ToString() + "," +
           playerController.perRoundEnemySpawnSpikeCount.ToString() + "," +
           playerController.degreeToShootXCumulative.ToString() + "," +
           playerController.degreeToTargetXCumulative.ToString() + "," +
           playerController.minAnlgeToEnemyCumulative.ToString() + "," +
           playerController.enemySizeCumulative.ToString() + "," +
           playerController.timeToTargetEnemyCumulative.ToString() + "," +
           playerController.timeToHitEnemyCumulative.ToString() + "," +
           playerController.timeToKillEnemyCumulative.ToString() + "," +
           degXShootAvg.ToString() + "," +
           degXTargetAvg.ToString() + "," +
           enemySizeOnSpawnAvg.ToString() + "," +
           playerController.aimDurationPerRound.ToString() + "," +
           qoeValue.ToString()
            ;
        textWriter.WriteLine(roundLogLine);
        textWriter.Close();
    }

    public void LogPlayerData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        TextWriter textWriter = null;
        filenamePerRound = "Data\\Logs\\PlayerData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";
        while (textWriter == null)
            textWriter = System.IO.File.AppendText(filenamePerRound);

        for (int i = 0; i < playerController.playerTickLog.mouseX.Count; i++)
        {
            String tickLogLine =
               sessionID.ToString() + "," +
               currentRoundNumber.ToString() + "," +
               roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]].ToString() + "," +
               roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]].ToString() + "," +
               roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
               roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
               roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
               roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
               indexArray[currentRoundNumber - 1].ToString() + "," +
               playerController.playerTickLog.roundTimer[i].ToString() + "," +
               playerController.playerTickLog.time[i].ToString() + "," +
               playerController.playerTickLog.mouseX[i].ToString() + "," +
               playerController.playerTickLog.mouseY[i].ToString() + "," +
               playerController.playerTickLog.playerX[i].ToString() + "," +
               playerController.playerTickLog.playerY[i].ToString() + "," +
               playerController.playerTickLog.playerZ[i].ToString() + "," +
               playerController.playerTickLog.scorePerSec[i].ToString() + "," +
               playerController.playerTickLog.playerRot[i].ToString() + "," +
               playerController.playerTickLog.enemyPos[i].ToString() + "," +
               playerController.playerTickLog.isADS[i].ToString();

            textWriter.WriteLine(tickLogLine);
        }
        textWriter.Close();

    }
}
