using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;

public class Env : MonoBehaviour
{
    public Color fullEnergyColor;
    public Color cellColor1;    //2-5
    public Color cellColor2;    //5-10
    public Color cellColor3;  //>10
    public Color cellColor4;
    //public Color cellColor0;


    public GameObject gridPrefab;
    public GameObject cellPrefab;
    public GameObject cellCubePrefab;
    
    

    public int colNum;
    public int rowNum;
    public int highNum;

    public int initNum;
    
    [Range(0,200)]
    public float initMotherEnergy;
    public float gridInitEnergy;
    public float gridMaxEnergy;
    

    private SingleGrid[,,] grids;//三维数组
    
    public static Env Instance { get; private set; } // Env.Instance.方法 可以在其他类里调用
    public int currentRound;
    public int cellNum;
    private int cellNum1;
    private int cellNum2;
    public int overcrowdedNum = 8;
    public int overcrowdedNumUp = 8;
    public int initialPorosity;
    public int newAddedCell;
    public int newReducedCell;
    public float speed = 0.1f;
    public int materialNumber;

    private float currentStep;
    private float latestDropAge;
    private float ageOfCubeMaterial;
    private double evap=8*Mathf.Pow(10,-7);

    private bool simulationEnabled = true;
    private bool start = false;

    public float interval=1;
    private int a=0,b=0, c = 0;
    public int maxmount=1000;
    public int totalMaterial;
    public int spacing;
    public int restMaterial;
    public float p;
    public int exmaterial;

    List<SingleGrid> allPrefab = null;//存储地下减掉能量的网格
    List<SingleGrid> UndergroundGridReduced = null;
    List<SingleGrid> AboveGroundCellSpawned = null;
    List<SingleGrid> PorosityDLA = null;
    List<SingleGrid> PorosityVoidGrid = null;
    List<SingleGrid> PorosityGrid = null;

    public int reduceGrid;
    public int maxReduce;
    private SingleGrid initialReduceGrid;
    public SingleGrid initialAgent;
    public int newPrefab;
    public int prefabNum;
    //public int holeNum=50;
    

    public void Awake()
    {
        if (Instance != null)
            Destroy(this);
        Instance = this;
    }

    private void Start()
    {
        allPrefab = new List<SingleGrid>();
        UndergroundGridReduced = new List<SingleGrid>();
        AboveGroundCellSpawned = new List<SingleGrid>();
        PorosityDLA= new List<SingleGrid>();
        PorosityVoidGrid= new List<SingleGrid>();
        PorosityGrid = new List<SingleGrid>();

        InitGrids();
        CountTotalMaterial();
        initialPorosity = overcrowdedNumUp;
        SingleGrid initialPrefab = NumUpGrid(RandomGetGridWithoutCellInEnv(), 2);
        allPrefab.Add(initialPrefab);
        newPrefab++;

        //StartSimulate();
        //AddOrReduceEnergy();

    }
    private void Update()
    {
        //if(start==false)

        //  AddDLAAgent();
        if (simulationEnabled )
        //if (simulationEnabled )
        {
            //StartSimulate();
            Run();
        }
        exmaterial = cellNum;

        CountRestMaterial();
        CountMaterialEfficiency();
        UserInput();
        CheckEnvironmentCondition();
    }


    //InitialCondition Setting
    private void CountTotalMaterial()
    {
        
            for (int i = 0; i < colNum; i++)
                for (int j = 0; j < rowNum; j++)
                    for (int k = 0; k < highNum; k++)
                    {

                        if (grids[i, j, k].gridEnergy == 50)
                            totalMaterial++;

                    }
        

        
    }
    private int CountRestMaterial()
    {
        int a = 0;
            for (int i = 0; i < colNum; i++)
                for (int j = 0; j < rowNum; j++)
                    for (int k = 0; k < highNum; k++)
                    {

                    if (grids[i, j, k].gridEnergy == 50)
                        a++;

                    }
        

        return restMaterial=a;
    }
    private void CountMaterialEfficiency()
    {


        p = Mathf.RoundToInt(((float)exmaterial / totalMaterial)*100);
    }
    private void RemoveVoidGrid()
    {
        //if (Input.GetKeyDown(KeyCode.C))
        //{
            
        //    for (int i = 0; i < colNum; i++)
        //        for (int j = 0; j < rowNum; j++)
        //            for (int k = 0; k < highNum; k++)
        //            {
        //                if (grids[i, j, k].gridEnergy == 0 && !grids[i, j, k].HasCell())
        //                {
        //                    Destroy(grids[i, j, k].gameObject);
                            
        //                }
                        
        //}

        for (int i = 0; i < colNum; i++)
            for (int j = 0; j < rowNum; j++)
                for (int k = 0; k < highNum; k++)
                {
                    
                    grids[i, j, k].IfRender();
                }
    }
    private void UserInput()
    {
        if (Input.GetKeyUp(KeyCode.C))
        {
            ClearDLA();
        }

        if (Input.GetKeyUp(KeyCode.A))
        {
            overcrowdedNumUp++;
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            overcrowdedNumUp--;
        }

        if (Input.GetKeyUp(KeyCode.D))
        {
            DisPlayDLA();
        }
    }
    
    
    private void InitCells(int initNumber)//根据数量生成初始母细胞
    {
        List<SingleGrid> initialCell = new List<SingleGrid>();
        for (int i = 0; i < initNumber;)
        {

            initialAgent = RandomGetGridWithoutCellInEnv();
            //防止初始生成位置重复
            //如果g属于list里的 就重新随机 直到不重复   
            if (initialCell.IndexOf(initialAgent) >= 0)
            {
                continue;
            }
            else
            {

                initialAgent.SpawnCell(initMotherEnergy);
                //allPrefab.Add(g);//初始母细胞加入list
                initialCell.Add(initialAgent);
                i++;
            }


        }
        //CalculateGridsEnergy();
        //CalculateCellsEnergy();
    }

    public SingleGrid RandomGetGridWithoutCellInEnv()  //just for initial 随机选出没有细胞的网格   初始细胞生成的位置 天空 慢慢降落到地上 agent 底下网格含有建筑材料
    {
        int rangeX, rangeY, rangeZ;
        SingleGrid randomGrid;
        while (true)
        {
            //rangeX = UnityEngine.Random.Range(colNum / 3, colNum*2/3);
            rangeX =colNum/3;
            //rangeZ = UnityEngine.Random.Range(0, highNum);
            rangeZ = highNum/3;
            //rangeX = 0;
            //rangeY = (rowNum / materialNumber) + 1;
            rangeY = UnityEngine.Random.Range(0, rowNum); 
            randomGrid = grids[rangeX, rangeY, rangeZ];
            if (randomGrid != null && !randomGrid.HasCell() && randomGrid.gridEnergy==0 && BottomGrid(randomGrid).gridEnergy==50)//网格没有细胞并且在cube里 
                return randomGrid;
        }
    }
    void InitGrids()//初始能量分布
    {
        grids = new SingleGrid[colNum, rowNum,highNum];//网格 能量场形状 初始化
        Vector3 pos=new Vector3(20,0,20);
        
        for (int i =0;i<colNum;i++)
            for (int j = 0; j < rowNum; j++)
                for(int k = 0;k<highNum;k++)
            {
                    //pos.x = i;
                    //pos.y = j;
                    //pos.z = k;
                    //GameObject go = Instantiate(gridPrefab, pos, Quaternion.identity, transform);//克隆目标物体
                    //go.name = "grid(" + i.ToString() + ',' + j.ToString() + ',' + k.ToString() + ')';
                    //grids[i, j, k] = go.GetComponent<SingleGrid>();// 向每个三维坐标 注入 实例化网格 go 
                    //grids[i, j, k].AddGridEnergy(gridInitEnergy);//每个网格注入能量
                    //grids[i, j, k].x = i;
                    //grids[i, j, k].y = j;
                    //grids[i, j, k].z = k;

                    //if ((i <= colNum && j <= rowNum * 1 / materialNumber && k <= highNum)||(i<=colNum*3/5 && i>=colNum*2/5 && j<=rowNum && k <= highNum * 3 / 5 && k >= highNum * 2 / 5) )
                    //if (i <= colNum && j <= rowNum * 1 / materialNumber && k <= highNum)
                    //if ((i <= colNum && j <= rowNum / materialNumber && k <= highNum)
                    //    || (i >= 0 && i <= 48 && j == 11 && k == 0) || (i >= 0 && i <= 47 && j == 11 && k >= 1 && k <= 4) || (i >= 0 && i <= 46 && j == 11 && k == 5)
                    //    || (i >= 0 && i <= 45 && j == 11 && k == 6) || (i >= 0 && i <= 44 && j == 11 && k >= 7 && k <= 8) || (i >= 0 && i <= 43 && j == 11 && k == 9) || (i >= 0 && i <= 41 && j == 11 && k == 10)
                    //    || (i >= 0 && i <= 40 && j == 11 && k == 11) || (i >= 0 && i <= 39 && j == 11 && k >= 12 & k <= 19) || (i >= 0 && i <= 40 && j == 11 && k >= 20 && k <= 25) || (i >= 0 && i <= 41 && j == 11 && k >= 26 && k <= 27)
                    //    || (i >= 0 && i <= 40 && j == 11 && k == 28) || (i >= 0 && i <= 39 && j == 11 && k >= 29 && k <= 30) || (i >= 0 && i <= 38 && j == 11 && k >= 31 && k <= 32) || (i >= 0 && i <= 36 && j == 11 && k == 33)
                    //    || (i >= 0 && i <= 35 && j == 11 && k == 34) || (i >= 0 && i <= 33 && j == 11 && k == 35) || (i >= 0 && i <= 31 && j == 11 && k == 36) || (i >= 0 && i <= 30 && j == 11 && k == 37) || (i >= 0 && i <= 28 && j == 11 && k == 38)
                    //    || (i >= 0 && i <= 25 && j == 11 && k == 39) || (i >= 0 && i <= 23 && j == 11 && k == 40) || (i >= 0 && i <= 20 && j == 11 && k == 41) || (i >= 0 && i <= 16 && j == 11 && k == 42) || (i >= 0 && i <= 13 && j == 11 && k >= 43 && k <= 47)
                    //    || (i >= 0 && i <= 14 && j == 11 && k >= 48 && k <= 50) || (i >= 0 && i <= 16 && j == 11 && k >= 51 && k <= 52) || (i >= 0 && i <= 17 && j == 11 && k == 53) || (i >= 0 && i <= 18 && j == 11 && k == 54) || (i >= 23 && i <= 25 && j == 11 && k == 54)
                    //    || (i >= 0 && i <= 28 && j == 11 && k == 55)

                    //    || (i >= 0 && i <= 41 && j == 12 && k >= 0 && k <= 1) || (i >= 0 && i <= 40 && j == 12 && k == 2) || (i >= 0 && i <= 39 && j == 12 && k == 3) || (i >= 0 && i <= 38 && j == 12 && k == 4) || (i >= 0 && i <= 37 && j == 12 && k == 5)
                    //    || (i >= 0 && i <= 36 && j == 12 && k == 6) || (i >= 0 && i <= 35 && j == 12 && k == 7) || (i >= 0 && i <= 34 && j == 12 && k >= 8 && k <= 11) || (i >= 0 && i <= 35 && j == 12 && k >= 12 && k <= 18) || (i >= 0 && i <= 34 && j == 12 && k >= 19 && k <= 20)
                    //    || (i >= 0 && i <= 33 && j == 12 && k == 21) || (i >= 0 && i <= 32 && j == 12 && k == 22) || (i >= 0 && i <= 31 && j == 12 && k == 23) || (i >= 0 && i <= 29 && j == 12 && k == 24) || (i >= 0 && i <= 27 && j == 12 && k == 25) || (i >= 0 && i <= 26 && j == 12 && k == 26)
                    //    || (i >= 0 && i <= 23 && j == 12 && k == 27) || (i >= 0 && i <= 20 && j == 12 && k == 28) || (i >= 0 && i <= 17 && j == 12 && k == 29) || (i >= 0 && i <= 14 && j == 12 && k == 30) || (i >= 0 && i <= 13 && j == 12 && k == 31) || (i >= 0 && i <= 11 && j == 12 && k == 32)
                    //    || (i >= 0 && i <= 10 && j == 12 && k == 33) || (i >= 0 && i <= 8 && j == 12 && k == 34) || (i >= 0 && i <= 5 && j == 12 && k == 35) || (i >= 0 && i <= 2 && j == 12 && k == 36) || (i >= 0 && i <= 0 && j == 12 && k == 37)

                    //    || (i >= 0 && i <= 36 && j == 13 && k == 0) || (i >= 0 && i <= 35 && j == 13 && k == 1) || (i >= 0 && i <= 34 && j == 13 && k == 2) || (i >= 0 && i <= 33 && j == 13 && k == 3) || (i >= 0 && i <= 32 && j == 13 && k == 4) || (i >= 0 && i <= 31 && j == 13 && k >= 5 && k <= 10)
                    //    || (i >= 0 && i <= 32 && j == 13 && k >= 11 && k <= 12) || (i >= 0 && i <= 33 && j == 13 && k >= 13 && k <= 16) || (i >= 0 && i <= 32 && j == 13 && k >= 17 && k <= 18) || (i >= 0 && i <= 31 && j == 13 && k == 19) || (i >= 0 && i <= 30 && j == 13 && k == 20)
                    //    || (i >= 0 && i <= 29 && j == 13 && k == 21) || (i >= 0 && i <= 27 && j == 13 && k == 22) || (i >= 0 && i <= 22 && j == 13 && k == 23) || (i >= 0 && i <= 18 && j == 13 && k == 24) || (i >= 0 && i <= 15 && j == 13 && k == 25) || (i >= 0 && i <= 13 && j == 13 && k == 26)
                    //    || (i >= 0 && i <= 12 && j == 13 && k == 27) || (i >= 0 && i <= 11 && j == 13 && k == 28) || (i >= 0 && i <= 10 && j == 13 && k == 29) || (i >= 0 && i <= 9 && j == 13 && k == 30) || (i >= 0 && i <= 5 && j == 13 && k == 31) || (i >= 0 && i <= 2 && j == 13 && k == 32)

                    //    || (i >= 0 && i <= 32 && j == 14 && k == 0) || (i >= 0 && i <= 31 && j == 14 && k == 1) || (i >= 0 && i <= 30 && j == 14 && k >= 2 && k <= 3) || (i >= 0 && i <= 29 && j == 14 && k >= 4 && k <= 5) || (i >= 0 && i <= 28 && j == 14 && k >= 6 && k <= 9)
                    //    || (i >= 0 && i <= 29 && j == 14 && k >= 10 && k <= 11) || (i >= 0 && i <= 30 && j == 14 && k >= 12 && k <= 13) || (i >= 0 && i <= 31 && j == 14 && k >= 14 && k <= 16) || (i >= 0 && i <= 30 && j == 14 && k >= 17 && k <= 18) || (i >= 0 && i <= 28 && j == 14 && k == 19)
                    //    || (i >= 0 && i <= 26 && j == 14 && k == 20) || (i >= 0 && i <= 19 && j == 14 && k == 21) || (i >= 0 && i <= 12 && j == 14 && k == 22) || (i >= 0 && i <= 11 && j == 14 && k >= 23 && k <= 24) || (i >= 0 && i <= 10 && j == 14 && k == 25) || (i >= 0 && i <= 9 && j == 14 && k == 26)
                    //    || (i >= 0 && i <= 8 && j == 14 && k == 27) || (i >= 0 && i <= 7 && j == 14 && k == 28) || (i >= 0 && i <= 3 && j == 14 && k == 29)

                    //    || (i >= 0 && i <= 29 && j == 15 && k >= 0 && k <= 1) || (i >= 0 && i <= 28 && j == 15 && k == 2) || (i >= 0 && i <= 27 && j == 15 && k >= 3 && k <= 4) || (i >= 0 && i <= 26 && j == 15 && k >= 5 && k <= 9) || (i >= 0 && i <= 27 && j == 15 && k >= 10 && k <= 11)
                    //    || (i >= 0 && i <= 28 && j == 15 && k == 12) || (i >= 0 && i <= 29 && j == 15 && k >= 13 && k <= 16) || (i >= 0 && i <= 28 && j == 15 && k == 17) || (i >= 0 && i <= 26 && j == 15 && k == 18) || (i >= 18 && i <= 22 && j == 15 && k == 19) || (i >= 0 && i <= 16 && j == 15 && k == 19)
                    //    || (i >= 0 && i <= 10 && j == 15 && k >= 20 && k <= 21) || (i >= 0 && i <= 9 && j == 15 && k == 22) || (i >= 0 && i <= 8 && j == 15 && k == 23) || (i >= 0 && i <= 7 && j == 15 && k == 24) || (i >= 0 && i <= 6 && j == 15 && k == 25) || (i >= 0 && i <= 5 && j == 15 && k == 26)
                    //    || (i >= 0 && i <= 2 && j == 15 && k == 27)

                    //    || (i >= 0 && i <= 27 && j == 16 && k == 0) || (i >= 0 && i <= 26 && j == 16 && k >= 1 && k <= 2) || (i >= 0 && i <= 25 && j == 16 && k >= 3 && k <= 4) || (i >= 0 && i <= 24 && j == 16 && k >= 5 && k <= 10) || (i >= 0 && i <= 25 && j == 16 && k >= 11 && k <= 12)
                    //    || (i >= 0 && i <= 26 && j == 16 && k >= 13 && k <= 15) || (i >= 0 && i <= 25 && j == 16 && k == 16) || (i >= 22 && i <= 23 && j == 16 && k == 17) || (i >= 0 && i <= 13 && j == 16 && k == 17) || (i >= 0 && i <= 10 && j == 16 && k == 18) || (i >= 0 && i <= 9 && j == 16 && k == 19)
                    //    || (i >= 0 && i <= 8 && j == 16 && k >= 20 && k <= 21) || (i >= 0 && i <= 7 && j == 16 && k == 22) || (i >= 0 && i <= 6 && j == 16 && k == 23) || (i >= 0 && i <= 5 && j == 16 && k == 24) || (i >= 0 && i <= 2 && j == 16 && k == 25)

                    //    || (i >= 0 && i <= 25 && j == 17 && k >= 0 && k <= 1) || (i >= 0 && i <= 24 && j == 17 && k == 2) || (i >= 0 && i <= 23 && j == 17 && k >= 3 && k <= 4) || (i >= 0 && i <= 22 && j == 17 && k >= 5 && k <= 8) || (i >= 0 && i <= 21 && j == 17 && k == 9) || (i >= 0 && i <= 22 && j == 17 && k >= 10 && k <= 13)
                    //    || (i >= 20 && i <= 22 && j == 17 && k == 14) || (i >= 0 && i <= 15 && j == 17 && k == 14) || (i >= 0 && i <= 13 && j == 17 && k == 15) || (i >= 0 && i <= 10 && j == 17 && k == 16) || (i >= 0 && i <= 9 && j == 17 && k == 17) || (i >= 0 && i <= 8 && j == 17 && k == 18) || (i >= 0 && i <= 7 && j == 17 && k == 19)
                    //    || (i >= 0 && i <= 6 && j == 17 && k == 20) || (i >= 0 && i <= 5 && j == 17 && k == 21) || (i >= 0 && i <= 4 && j == 17 && k == 22) || (i >= 0 && i <= 2 && j == 17 && k == 23)

                    //    || (i >= 0 && i <= 23 && j == 18 && k >= 0 && k <= 1) || (i >= 0 && i <= 22 && j == 18 && k >= 2 && k <= 3) || (i >= 0 && i <= 21 && j == 18 && k == 4) || (i >= 0 && i <= 20 && j == 18 && k >= 5 && k <= 6) || (i >= 0 && i <= 19 && j == 18 && k >= 7 && k <= 8) || (i >= 0 && i <= 18 && j == 18 && k == 9)
                    //    || (i >= 0 && i <= 17 && j == 18 && k == 10) || (i >= 0 && i <= 16 && j == 18 && k == 11) || (i >= 0 && i <= 14 && j == 18 && k == 12) || (i >= 0 && i <= 12 && j == 18 && k == 13) || (i >= 0 && i <= 10 && j == 18 && k == 14) || (i >= 0 && i <= 9 && j == 18 && k == 15) || (i >= 0 && i <= 7 && j == 18 && k == 16)
                    //    || (i >= 0 && i <= 6 && j == 18 && k == 17) || (i >= 0 && i <= 5 && j == 18 && k == 18) || (i >= 0 && i <= 4 && j == 18 && k == 19) || (i >= 0 && i <= 3 && j == 18 && k == 20) || (i >= 0 && i <= 2 && j == 18 && k == 21) || (i >= 0 && i <= 1 && j == 18 && k == 22)

                    //    || (i >= 0 && i <= 21 && j == 19 && k == 0) || (i >= 0 && i <= 20 && j == 19 && k == 1) || (i >= 0 && i <= 19 && j == 19 && k >= 2 && k <= 3) || (i >= 0 && i <= 18 && j == 19 && k == 4) || (i >= 0 && i <= 17 && j == 19 && k >= 5 && k <= 6) || (i >= 0 && i <= 16 && j == 19 && k == 7)
                    //    || (i >= 0 && i <= 15 && j == 19 && k == 8) || (i >= 0 && i <= 14 && j == 19 && k == 9) || (i >= 0 && i <= 12 && j == 19 && k == 10) || (i >= 0 && i <= 10 && j == 19 && k == 11) || (i >= 0 && i <= 8 && j == 19 && k == 12) || (i >= 0 && i <= 7 && j == 19 && k == 13) || (i >= 0 && i <= 6 && j == 19 && k == 14)
                    //    || (i >= 0 && i <= 5 && j == 19 && k == 15) || (i >= 0 && i <= 4 && j == 19 && k == 16) || (i >= 0 && i <= 2 && j == 19 && k == 17) || (i >= 0 && i <= 1 && j == 19 && k >= 18 && k <= 19)

                    //    || (i >= 0 && i <= 18 && j == 20 && k >= 0 && k <= 1) || (i >= 0 && i <= 17 && j == 20 && k == 2) || (i >= 0 && i <= 16 && j == 20 && k == 3) || (i >= 0 && i <= 15 && j == 20 && k >= 4 && k <= 5) || (i >= 0 && i <= 14 && j == 20 && k == 6) || (i >= 0 && i <= 12 && j == 20 && k == 7) || (i >= 0 && i <= 10 && j == 20 && k == 8)
                    //    || (i >= 0 && i <= 7 && j == 20 && k == 9) || (i >= 0 && i <= 6 && j == 20 && k == 10) || (i >= 0 && i <= 5 && j == 20 && k == 11) || (i >= 0 && i <= 4 && j == 20 && k == 12) || (i >= 0 && i <= 3 && j == 20 && k == 13) || (i >= 0 && i <= 2 && j == 20 && k == 14) || (i >= 0 && i <= 1 && j == 20 && k == 15)

                    //    || (i >= 0 && i <= 16 && j == 21 && k == 0) || (i >= 0 && i <= 15 && j == 21 && k == 1) || (i >= 0 && i <= 14 && j == 21 && k >= 2 && k <= 3) || (i >= 0 && i <= 13 && j == 21 && k == 4) || (i >= 0 && i <= 11 && j == 21 && k == 5) || (i >= 0 && i <= 8 && j == 21 && k == 6) || (i >= 0 && i <= 7 && j == 21 && k == 7)
                    //    || (i >= 0 && i <= 5 && j == 21 && k == 8) || (i >= 0 && i <= 4 && j == 21 && k == 9) || (i >= 0 && i <= 3 && j == 21 && k == 10) || (i >= 0 && i <= 2 && j == 21 && k == 11) || (i >= 0 && i <= 1 && j == 21 && k == 12) || (i >= 0 && i <= 0 && j == 21 && k == 13)

                    //    || (i >= 0 && i <= 15 && j == 22 && k == 0) || (i >= 0 && i <= 14 && j == 22 && k == 1) || (i >= 0 && i <= 13 && j == 22 && k == 2) || (i >= 0 && i <= 12 && j == 22 && k == 3) || (i >= 0 && i <= 10 && j == 22 && k == 4) || (i >= 0 && i <= 8 && j == 22 && k == 5) || (i >= 0 && i <= 5 && j == 22 && k == 6)
                    //    || (i >= 0 && i <= 3 && j == 22 && k == 7) || (i >= 0 && i <= 2 && j == 22 && k == 8) || (i >= 0 && i <= 1 && j == 22 && k == 9) || (i >= 0 && i <= 0 && j == 22 && k == 10)

                    //    || (i >= 0 && i <= 13 && j == 23 && k == 0) || (i >= 0 && i <= 12 && j == 23 && k == 1) || (i >= 0 && i <= 11 && j == 23 && k == 2) || (i >= 0 && i <= 9 && j == 23 && k == 3) || (i >= 0 && i <= 7 && j == 23 && k == 4) || (i >= 0 && i <= 4 && j == 23 && k == 5) || (i >= 0 && i <= 2 && j == 23 && k == 6)
                    //    || (i >= 0 && i <= 1 && j == 23 && k == 7) || (i >= 0 && i <= 0 && j == 23 && k == 8)

                    //    || (i >= 0 && i <= 11 && j == 24 && k == 0) || (i >= 0 && i <= 10 && j == 24 && k == 1) || (i >= 0 && i <= 8 && j == 24 && k == 2) || (i >= 0 && i <= 6 && j == 24 && k == 3) || (i >= 0 && i <= 3 && j == 24 && k == 4) || (i >= 0 && i <= 1 && j == 24 && k == 5) || (i >= 0 && i <= 0 && j == 24 && k == 6)

                    //    || (i >= 0 && i <= 9 && j == 25 && k == 0) || (i >= 0 && i <= 7 && j == 25 && k == 1) || (i >= 0 && i <= 4 && j == 25 && k == 2) || (i >= 0 && i <= 2 && j == 25 && k == 3) || (i >= 0 && i <= 0 && j == 25 && k == 4)

                    //    || (i >= 0 && i <= 6 && j == 26 && k == 0) || (i >= 0 && i <= 4 && j == 26 && k == 1) || (i >= 0 && i <= 1 && j == 26 && k == 2)

                    //    || (i >= 0 && i <= 3 && j == 27 && k == 0) || (i >= 0 && i <= 1 && j == 27 && k == 1)

                    //    || (i >= 0 && i <= 0 && j == 28 && k == 0)
                    //    )

                    //if ((i <colNum && j < rowNum / materialNumber && k < highNum)
                    //    || (i >= 0 && i <= colNum - 8 && j == (rowNum / materialNumber) && k == 0) || (i >= 0 && i <= colNum - 9 && j == (rowNum / materialNumber)  && k >= 1 && k <= 4) || (i >= 0 && i <= colNum - 10 && j == (rowNum / materialNumber)  && k == 5)
                    //    || (i >= 0 && i <= colNum - 11 && j == (rowNum / materialNumber) && k == 6) || (i >= 0 && i <= colNum - 12 && j == (rowNum / materialNumber)  && k >= 7 && k <= 8) || (i >= 0 && i <= colNum - 13 && j == (rowNum / materialNumber) && k == 9) || (i >= 0 && i <= colNum - 15 && j == (rowNum / materialNumber)  && k == 10)
                    //    || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) && k == 11) || (i >= 0 && i <= colNum - 17 && j == (rowNum / materialNumber)  && k >= 12 & k <= 19) || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) && k >= 20 && k <= 25) || (i >= 0 && i <= colNum - 15 && j == (rowNum / materialNumber)  && k >= 26 && k <= 27)
                    //    || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) && k == 28) || (i >= 0 && i <= colNum - 17 && j == (rowNum / materialNumber)  && k >= 29 && k <= 30) || (i >= 0 && i <= colNum - 18 && j == (rowNum / materialNumber) && k >= 31 && k <= 32) || (i >= 0 && i <= colNum - 20 && j == (rowNum / materialNumber) && k == 33)
                    //    || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) && k == 34) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) && k == 35) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) && k == 36) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) && k == 37) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) && k == 38)
                    //    || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) && k == 39) || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) && k == 40) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) && k == 41) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) && k == 42) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) && k >= 43 && k <= 47)
                    //    || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) && k >= 48 && k <= 50) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) && k >= 51 && k <= 52) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) && k == 53) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) && k == 54) || (i >= colNum - 33 && i <= colNum - 31 && j == (rowNum / materialNumber) && k == 54)
                    //    || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) && k == 55)

                    //    || (i >= 0 && i <= colNum - 15 && j == (rowNum / materialNumber) + 1 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) + 1 && k == 2) || (i >= 0 && i <= colNum - 17 && j == (rowNum / materialNumber) + 1 && k == 3) || (i >= 0 && i <= colNum - 18 && j == (rowNum / materialNumber) + 1 && k == 4) || (i >= 0 && i <= colNum - 19 && j == (rowNum / materialNumber) + 1 && k == 5)
                    //    || (i >= 0 && i <= colNum - 20 && j == (rowNum / materialNumber) + 1 && k == 6) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) + 1 && k == 7) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) + 1 && k >= 8 && k <= 11) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) + 1 && k >= 12 && k <= 18) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) + 1 && k >= 19 && k <= 20)
                    //    || (i >= 0 && i <= colNum - 23 && j == (rowNum / materialNumber) + 1 && k == 21) || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 1 && k == 22) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 1 && k == 23) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 1 && k == 24) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 1 && k == 25) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 1 && k == 26)
                    //    || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 1 && k == 27) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 1 && k == 28) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 1 && k == 29) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 1 && k == 30) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 1 && k == 31) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 1 && k == 32)
                    //    || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 1 && k == 33) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 1 && k == 34) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 1 && k == 35) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 1 && k == 36) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 1 && k == 37)

                    //    || (i >= 0 && i <= colNum - 20 && j == (rowNum / materialNumber) + 2 && k == 0) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) + 2 && k == 1) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) + 2 && k == 2) || (i >= 0 && i <= colNum - 23 && j == (rowNum / materialNumber) + 2 && k == 3) || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 2 && k == 4) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 2 && k >= 5 && k <= 10)
                    //    || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 2 && k >= 11 && k <= 12) || (i >= 0 && i <= colNum - 23 && j == (rowNum / materialNumber) + 2 && k >= 13 && k <= 16) || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 2 && k >= 17 && k <= 18) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 2 && k == 19) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 2 && k == 20)
                    //    || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 2 && k == 21) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 2 && k == 22) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 2 && k == 23) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 2 && k == 24) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 2 && k == 25) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 2 && k == 26)
                    //    || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 2 && k == 27) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 2 && k == 28) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 2 && k == 29) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 2 && k == 30) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 2 && k == 31) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 2 && k == 32)

                    //    || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 3 && k == 0) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 3 && k == 1) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 3 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 3 && k >= 4 && k <= 5) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 3 && k >= 6 && k <= 9)
                    //    || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 3 && k >= 10 && k <= 11) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 3 && k >= 12 && k <= 13) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 3 && k >= 14 && k <= 16) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 3 && k >= 17 && k <= 18) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 3 && k == 19)
                    //    || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 3 && k == 20) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 3 && k == 21) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 3 && k == 22) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 3 && k >= 23 && k <= 24) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 3 && k == 25) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 3 && k == 26)
                    //    || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 3 && k == 27) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 3 && k == 28) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 3 && k == 29)

                    //    || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 4 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 4 && k == 2) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 4 && k >= 3 && k <= 4) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 4 && k >= 5 && k <= 9) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 4 && k >= 10 && k <= 11)
                    //    || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 4 && k == 12) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 4 && k >= 13 && k <= 16) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 4 && k == 17) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 4 && k == 18) || (i >= colNum - 38 && i <= colNum - 34 && j == (rowNum / materialNumber) + 4 && k == 19) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 4 && k == 19)
                    //    || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 4 && k >= 20 && k <= 21) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 4 && k == 22) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 4 && k == 23) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 4 && k == 24) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 4 && k == 25) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 4 && k == 26)
                    //    || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 4 && k == 27)

                    //    || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 5 && k == 0) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 5 && k >= 1 && k <= 2) || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 5 && k >= 3 && k <= 4) || (i >= 0 && i <= colNum - 32 && j == (rowNum / materialNumber) + 5 && k >= 5 && k <= 10) || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 5 && k >= 11 && k <= 12)
                    //    || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 5 && k >= 13 && k <= 15) || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 5 && k == 16) || (i >= colNum - 34 && i <= colNum - 33 && j == (rowNum / materialNumber) + 5 && k == 17) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 5 && k == 17) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 5 && k == 18) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 5 && k == 19)
                    //    || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 5 && k >= 20 && k <= 21) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 5 && k == 22) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 5 && k == 23) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 5 && k == 24) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 5 && k == 25)

                    //    || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 6 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 32 && j == (rowNum / materialNumber) + 6 && k == 2) || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 6 && k >= 3 && k <= 4) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 6 && k >= 5 && k <= 8) || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 6 && k == 9) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 6 && k >= 10 && k <= 13)
                    //    || (i >= colNum - 36 && i <= colNum - 34 && j == (rowNum / materialNumber) + 6 && k == 14) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 6 && k == 14) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 6 && k == 15) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 6 && k == 16) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 6 && k == 17) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 6 && k == 18) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 6 && k == 19)
                    //    || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 6 && k == 20) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 6 && k == 21) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 6 && k == 22) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 6 && k == 23)

                    //    || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 7 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 7 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 7 && k == 4) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 7 && k >= 5 && k <= 6) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 7 && k >= 7 && k <= 8) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 7 && k == 9)
                    //    || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 7 && k == 10) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 7 && k == 11) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 7 && k == 12) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 7 && k == 13) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 7 && k == 14) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 7 && k == 15) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 7 && k == 16)
                    //    || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 7 && k == 17) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 7 && k == 18) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 7 && k == 19) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 7 && k == 20) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 7 && k == 21) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 7 && k == 22)

                    //    || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 8 && k == 0) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 8 && k == 1) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 8 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 8 && k == 4) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 8 && k >= 5 && k <= 6) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 8 && k == 7)
                    //    || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 8 && k == 8) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 8 && k == 9) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 8 && k == 10) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 8 && k == 11) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 8 && k == 12) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 8 && k == 13) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 8 && k == 14)
                    //    || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 8 && k == 15) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 8 && k == 16) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 8 && k == 17) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 8 && k >= 18 && k <= 19)

                    //    || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 9 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 9 && k == 2) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 9 && k == 3) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 9 && k >= 4 && k <= 5) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 9 && k == 6) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 9 && k == 7) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 9 && k == 8)
                    //    || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 9 && k == 9) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 9 && k == 10) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 9 && k == 11) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 9 && k == 12) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 9 && k == 13) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 9 && k == 14) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 9 && k == 15)

                    //    || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 10 && k == 0) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 10 && k == 1) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 10 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 10 && k == 4) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 10 && k == 5) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 10 && k == 6) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 10 && k == 7)
                    //    || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 10 && k == 8) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 10 && k == 9) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 10 && k == 10) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 10 && k == 11) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 10 && k == 12) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 10 && k == 13)

                    //    || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 11 && k == 0) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 11 && k == 1) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 11 && k == 2) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 11 && k == 3) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 11 && k == 4) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 11 && k == 5) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 11 && k == 6)
                    //    || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 11 && k == 7) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 11 && k == 8) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 11 && k == 9) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 11 && k == 10)

                    //    || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 12 && k == 0) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 12 && k == 1) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 12 && k == 2) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 12 && k == 3) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 12 && k == 4) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 12 && k == 5) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 12 && k == 6)
                    //    || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 12 && k == 7) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 12 && k == 8)

                    //    || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 13 && k == 0) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 13 && k == 1) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 13 && k == 2) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 13 && k == 3) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 13 && k == 4) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 13 && k == 5) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 13 && k == 6)

                    //    || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 14 && k == 0) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 14 && k == 1) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 14 && k == 2) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 14 && k == 3) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 14 && k == 4)

                    //    || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 15 && k == 0) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 15 && k == 1) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 15 && k == 2)

                    //    || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 16 && k == 0) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 16 && k == 1)

                    //    || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 17 && k == 0)
                    //    )

                    if ((i < colNum && j < rowNum / materialNumber && k < highNum)
                        || (i >= 0 && i <= colNum - 8 && j == (rowNum / materialNumber) && k == 0) || (i >= 0 && i <= colNum - 9 && j == (rowNum / materialNumber) && k >= 1 && k <= 4) || (i >= 0 && i <= colNum - 10 && j == (rowNum / materialNumber) && k == 5)
                        || (i >= 0 && i <= colNum - 11 && j == (rowNum / materialNumber) && k == 6) || (i >= 0 && i <= colNum - 12 && j == (rowNum / materialNumber) && k >= 7 && k <= 8) || (i >= 0 && i <= colNum - 13 && j == (rowNum / materialNumber) && k == 9) || (i >= 0 && i <= colNum - 15 && j == (rowNum / materialNumber) && k == 10)
                        || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) && k == 11) || (i >= 0 && i <= colNum - 17 && j == (rowNum / materialNumber) && k >= 12 & k <= 19) || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) && k >= 20 && k <= 25) || (i >= 0 && i <= colNum - 15 && j == (rowNum / materialNumber) && k >= 26 && k <= 27)
                        || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) && k == 28) || (i >= 0 && i <= colNum - 17 && j == (rowNum / materialNumber) && k >= 29 && k <= 30) || (i >= 0 && i <= colNum - 18 && j == (rowNum / materialNumber) && k >= 31 && k <= 32) || (i >= 0 && i <= colNum - 20 && j == (rowNum / materialNumber) && k == 33)
                        || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) && k == 34) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) && k == 35) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) && k == 36) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) && k == 37) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) && k == 38)
                        || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) && k == 39) || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) && k == 40) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) && k == 41) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) && k == 42) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) && k >= 43 && k <= 47)
                        || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) && k >= 48 && k <= 50) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) && k >= 51 && k <= 52) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) && k == 53) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) && k == 54) || (i >= colNum - 33 && i <= colNum - 31 && j == (rowNum / materialNumber) && k == 54)
                        || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) && k == 55) || (i== colNum - 8 && j == (rowNum / materialNumber) && k == 55) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) && k == 56) || (i >= colNum - 10 && i <= colNum - 9 && j == (rowNum / materialNumber) && k == 56) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) && k == 57) || (i >= colNum - 12 && i <= colNum - 9 && j == (rowNum / materialNumber) && k == 57) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) && k == 58) || (i >= colNum - 14 && i <= colNum - 9 && j == (rowNum / materialNumber) && k == 58)
                        || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) && k == 59) || (i >= colNum - 17 && i <= colNum - 10 && j == (rowNum / materialNumber) && k == 59) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) && k == 60) || (i >= colNum - 19 && i <= colNum - 10 && j == (rowNum / materialNumber) && k == 60) || (i >= 0 && i <= colNum - 11 && j == (rowNum / materialNumber) && k >= 61&&k<=62)
                        || (i >= 0 && i <= colNum - 12 && j == (rowNum / materialNumber) && k >= 63 && k <= 65) || (i >= 0 && i <= colNum - 13 && j == (rowNum / materialNumber) && k >= 66 && k <= 67) || (i >= 0 && i <= colNum - 14 && j == (rowNum / materialNumber) && k >= 68 && k <= 69) || (i >= 0 && i <= colNum - 15 && j == (rowNum / materialNumber) && k >= 70 && k <= 72) || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) && k >= 73 && k <= 75) || (i >= 0 && i <= colNum - 17 && j == (rowNum / materialNumber) && k ==76) || (i >= 0 && i <= colNum - 18 && j == (rowNum / materialNumber) && k >= 77 && k <= 78)
                        || (i >= 0 && i <= colNum - 19 && j == (rowNum / materialNumber) && k == 79) || (i >= 0 && i <= colNum - 20 && j == (rowNum / materialNumber) && k == 80) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) && k == 81) || (i >= 0 && i <= colNum - 23 && j == (rowNum / materialNumber) && k == 82) || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) && k == 83) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) && k == 84) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) && k == 85)




                        || (i >= 0 && i <= colNum - 15 && j == (rowNum / materialNumber) + 1 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 16 && j == (rowNum / materialNumber) + 1 && k == 2) || (i >= 0 && i <= colNum - 17 && j == (rowNum / materialNumber) + 1 && k == 3) || (i >= 0 && i <= colNum - 18 && j == (rowNum / materialNumber) + 1 && k == 4) || (i >= 0 && i <= colNum - 19 && j == (rowNum / materialNumber) + 1 && k == 5)
                        || (i >= 0 && i <= colNum - 20 && j == (rowNum / materialNumber) + 1 && k == 6) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) + 1 && k == 7) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) + 1 && k >= 8 && k <= 11) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) + 1 && k >= 12 && k <= 18) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) + 1 && k >= 19 && k <= 20)
                        || (i >= 0 && i <= colNum - 23 && j == (rowNum / materialNumber) + 1 && k == 21) || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 1 && k == 22) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 1 && k == 23) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 1 && k == 24) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 1 && k == 25) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 1 && k == 26)
                        || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 1 && k == 27) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 1 && k == 28) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 1 && k == 29) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 1 && k == 30) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 1 && k == 31) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 1 && k == 32)
                        || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 1 && k == 33) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 1 && k == 34) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 1 && k == 35) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 1 && k == 36) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 1 && k >= 37&&k<=38) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 1 && k >= 39 && k <= 40)
                        || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 1 && k >= 41 && k <= 42) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 1 && k >= 43 && k <= 44) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 1 && k >= 45 && k <= 46) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 1 && k >= 47 && k <= 48) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 1 && k >= 49 && k <= 51)
                        || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 1 && k ==52) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 1 && k == 53) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 1 && k == 54) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 1 && k == 55) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 1 && k == 56) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 1 && k == 57)
                        || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 1 && k == 58) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 1 && k == 59) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 1 && k == 60) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 1 && k == 61) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 1 && k == 62) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 1 && k >=63&&k<=64)
                        || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 1 && k == 65) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 1 && k >= 66 && k <= 67) || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 1 && k == 68) || (i >= 0 && i <= colNum - 32 && j == (rowNum / materialNumber) + 1 && k >= 69 && k <= 71) || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 1 && k >= 72 && k <= 80) || (i >= 0 && i <= colNum - 32 && j == (rowNum / materialNumber) + 1 && k >= 81 && k <= 85)


                        || (i >= 0 && i <= colNum - 20 && j == (rowNum / materialNumber) + 2 && k == 0) || (i >= 0 && i <= colNum - 21 && j == (rowNum / materialNumber) + 2 && k == 1) || (i >= 0 && i <= colNum - 22 && j == (rowNum / materialNumber) + 2 && k == 2) || (i >= 0 && i <= colNum - 23 && j == (rowNum / materialNumber) + 2 && k == 3) || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 2 && k == 4) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 2 && k >= 5 && k <= 10)
                        || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 2 && k >= 11 && k <= 12) || (i >= 0 && i <= colNum - 23 && j == (rowNum / materialNumber) + 2 && k >= 13 && k <= 16) || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 2 && k >= 17 && k <= 18) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 2 && k == 19) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 2 && k == 20)
                        || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 2 && k == 21) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 2 && k == 22) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 2 && k == 23) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 2 && k == 24) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 2 && k == 25) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 2 && k == 26)
                        || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 2 && k == 27) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 2 && k == 28) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 2 && k == 29) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 2 && k == 30) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 2 && k == 31) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 2 && k == 32)
                        || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 2 && k == 33) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 2 && k == 34) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 2 && k >= 35&&k<=37) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 2 && k >= 38 && k <= 39)||(i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 2 && k == 40) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 2 && k >= 41 && k <= 42)
                        || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 2 && k == 43) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 2 && k == 44) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 2 && k == 45) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 2 && k == 46) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 2 && k == 47) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 2 && k >= 48 && k <= 49)
                        || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 2 && k == 50) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 2 && k >= 51 && k <= 52) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 2 && k == 53) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 2 && k == 54) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 2 && k == 55) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 2 && k == 56)
                        || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 2 && k == 57) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 2 && k == 58) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 2 && k == 59) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 2 && k == 60) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 2 && k == 61) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 2 && k == 62)
                        || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 2 && k == 63) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 2 && k == 64) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 2 && k >=65&&k<=66) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 2 && k >= 67 && k <= 68) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 2 && k >= 69 && k <= 70)
                        || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 2 && k == 71) || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 2 && k >= 72 && k <= 73) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 2 && k == 74) || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 2 && k >= 75 && k <= 78) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 2 && k == 79)
                        || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 2 && k == 80) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 2 && k == 81) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 2 && k >= 82 && k <= 85)


                        || (i >= 0 && i <= colNum - 24 && j == (rowNum / materialNumber) + 3 && k == 0) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 3 && k == 1) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 3 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 3 && k >= 4 && k <= 5) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 3 && k >= 6 && k <= 9)
                        || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 3 && k >= 10 && k <= 11) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 3 && k >= 12 && k <= 13) || (i >= 0 && i <= colNum - 25 && j == (rowNum / materialNumber) + 3 && k >= 14 && k <= 16) || (i >= 0 && i <= colNum - 26 && j == (rowNum / materialNumber) + 3 && k >= 17 && k <= 18) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 3 && k == 19)
                        || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 3 && k == 20) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 3 && k == 21) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 3 && k == 22) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 3 && k >= 23 && k <= 24) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 3 && k == 25) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 3 && k == 26)
                        || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 3 && k == 27) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 3 && k == 28) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 3 && k == 29) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 3 && k == 30) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 3 && k == 31) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 3 && k == 32)
                        || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 3 && k >= 33 && k <= 35) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 3 && k >= 36 && k <= 37) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 3 && k >= 38 && k <= 39) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 3 && k == 40) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 3 && k == 41) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 3 && k == 42)
                        || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 3 && k == 43) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 3 && k == 44) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 3 && k >= 45 && k <= 48) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 3 && k >= 49 && k <= 53) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 3 && k == 54) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 3 && k == 55)
                        || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 3 && k == 56) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 3 && k == 57) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 3 && k == 58) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 3 && k == 59) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 3 && k == 60) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 3 && k == 61)
                        || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 3 && k == 62) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 3 && k >= 63 && k <= 64) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 3 && k == 65) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 3 && k == 66) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 3 && k >= 67 && k <= 68) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 3 && k >= 69&& k <= 71)
                        || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 3 && k >= 72 && k <= 74) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 3 && k >= 75 && k <= 76) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 3 && k == 77) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 3 && k == 78) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 3 && k == 79) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 3 && k >= 80 && k <= 81)
                        || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 3 && k >= 82 && k <= 85)




                        || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 4 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 4 && k == 2) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 4 && k >= 3 && k <= 4) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 4 && k >= 5 && k <= 9) || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 4 && k >= 10 && k <= 11)
                        || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 4 && k == 12) || (i >= 0 && i <= colNum - 27 && j == (rowNum / materialNumber) + 4 && k >= 13 && k <= 16) || (i >= 0 && i <= colNum - 28 && j == (rowNum / materialNumber) + 4 && k == 17) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 4 && k == 18) || (i >= colNum - 38 && i <= colNum - 34 && j == (rowNum / materialNumber) + 4 && k == 19) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 4 && k == 19)
                        || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 4 && k >= 20 && k <= 21) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 4 && k == 22) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 4 && k == 23) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 4 && k == 24) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 4 && k == 25) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 4 && k == 26)
                        || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 4 && k == 27) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 4 && k == 28) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 4 && k == 29) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 4 && k == 30) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 4 && k == 31) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 4 && k >= 32 && k <= 33)
                        || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 4 && k >= 34 && k <= 35) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 4 && k >= 36 && k <= 37) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 4 && k >= 38 && k <= 39) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 4 && k == 40) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 4 && k == 41) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 4 && k == 42)
                        || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 4 && k >= 43 && k <= 44) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 4 && k >= 45 && k <= 48) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 4 && k >= 49 && k <= 55) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 4 && k == 56) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 4 && k == 57) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 4 && k == 58)
                        || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 4 && k == 59) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 4 && k == 60) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 4 && k == 61) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 4 && k == 62) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 4 && k == 63) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 4 && k == 64)
                        || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 4 && k == 65) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 4 && k >= 66 && k <= 67) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 4 && k ==68) || (i >= 0 && i <= colNum - 43&& j == (rowNum / materialNumber) + 4 && k >= 69 && k <= 70) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 4 && k >= 71 && k <= 73)
                        || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 4 && k == 74) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 4 && k == 75) || (i >= 0 && i <= colNum - 45&& j == (rowNum / materialNumber) + 4 && k == 76) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 4 && k >= 77 && k <= 78) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 4 && k >= 79 && k <= 83) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 4 && k >= 84 && k <= 85)





                        || (i >= 0 && i <= colNum - 29 && j == (rowNum / materialNumber) + 5 && k == 0) || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 5 && k >= 1 && k <= 2) || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 5 && k >= 3 && k <= 4) || (i >= 0 && i <= colNum - 32 && j == (rowNum / materialNumber) + 5 && k >= 5 && k <= 10) || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 5 && k >= 11 && k <= 12)
                        || (i >= 0 && i <= colNum - 30 && j == (rowNum / materialNumber) + 5 && k >= 13 && k <= 15) || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 5 && k == 16) || (i >= colNum - 34 && i <= colNum - 33 && j == (rowNum / materialNumber) + 5 && k == 17) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 5 && k == 17) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 5 && k == 18) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 5 && k == 19)
                        || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 5 && k >= 20 && k <= 21) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 5 && k == 22) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 5 && k == 23) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 5 && k == 24) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 5 && k == 25)
                        || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 5 && k >= 26 && k <= 27) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 5 && k >= 28&& k <= 29) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 5 && k >= 30 && k <= 31) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 5 && k >= 32 && k <= 34) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 5 && k >= 35 && k <= 36)
                        || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 5 && k == 37) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 5 && k == 38) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 5 && k == 39) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 5 && k == 40) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 5 && k == 41) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 5 && k == 42)
                        || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 5 && k >= 43 && k <= 49) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 5 && k >= 50 && k <= 55) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 5 && k >= 56 && k <= 57) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 5 && k == 58) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 5 && k == 59) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 5 && k == 60)
                        || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 5 && k == 61) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 5 && k == 62) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 5 && k == 63) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 5 && k == 64) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 5 && k == 65) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 5 && k == 66)
                        || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 5 && k >= 67 && k <= 68) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 5 && k == 69) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 5 && k >= 70 && k <= 72) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 5 && k ==73) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 5 && k >= 74 && k <= 75) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 5 && k ==76)
                        || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 5 && k >= 77 && k <= 81) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 5 && k >= 82 && k <= 85)




                        || (i >= 0 && i <= colNum - 31 && j == (rowNum / materialNumber) + 6 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 32 && j == (rowNum / materialNumber) + 6 && k == 2) || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 6 && k >= 3 && k <= 4) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 6 && k >= 5 && k <= 8) || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 6 && k == 9) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 6 && k >= 10 && k <= 13)
                        || (i >= colNum - 36 && i <= colNum - 34 && j == (rowNum / materialNumber) + 6 && k == 14) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 6 && k == 14) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 6 && k == 15) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 6 && k == 16) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 6 && k == 17) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 6 && k == 18) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 6 && k == 19)
                        || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 6 && k == 20) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 6 && k == 21) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 6 && k == 22) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 6 && k == 23) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 6 && k >= 24 && k <= 25) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 6 && k == 26) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 6 && k >= 27 && k <= 28)
                        || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 6 && k >= 29 && k <= 31) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 6 && k >= 32 && k <= 33) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 6 && k >= 34 && k <= 36) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 6 && k == 37)|| (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 6 && k == 38) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 6 && k == 39) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 6 && k == 40)
                        || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 6 && k >= 41 && k <= 57) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 6 && k == 58) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 6 && k == 59) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 6 && k == 60) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 6 && k == 61) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 6 && k == 62)
                        || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 6 && k == 63) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 6 && k == 64) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 6 && k == 65) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 6 && k == 66) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 6 && k == 67) || (i >= 0 && i <= colNum - 52&& j == (rowNum / materialNumber) + 6 && k == 68) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 6 && k == 69)
                        || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 6 && k == 70) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 6 && k >= 71 && k <= 72) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 6 && k >= 73 && k <= 74) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 6 && k >= 75 && k <= 77) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 6 && k >= 78 && k <= 82) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 6 && k >= 83 && k <= 85)



                        || (i >= 0 && i <= colNum - 33 && j == (rowNum / materialNumber) + 7 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 34 && j == (rowNum / materialNumber) + 7 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 7 && k == 4) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 7 && k >= 5 && k <= 6) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 7 && k >= 7 && k <= 8) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 7 && k == 9)
                        || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 7 && k == 10) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 7 && k == 11) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 7 && k == 12) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 7 && k == 13) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 7 && k == 14) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 7 && k == 15) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 7 && k == 16)
                        || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 7 && k == 17) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 7 && k == 18) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 7 && k == 19) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 7 && k == 20) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 7 && k == 21) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 7 && k == 22) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 7 && k == 23)
                        || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 7 && k == 24) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 7 && k >=25&&k<=32) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 7 && k == 33) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 7 && k >= 34 && k <= 35) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 7 && k == 36) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 7 && k == 37)
                        || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 7 && k == 38) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 7 && k == 39) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 7 && k >= 40 && k <= 41) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 7 && k >= 42 && k <= 43) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 7 && k >= 44 && k <= 45) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 7 && k >= 46 && k <= 58)
                        || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 7 && k == 59) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 7 && k == 60) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 7 && k == 61) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 7 && k == 62) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 7 && k == 63) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 7 && k == 64) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 7 && k == 65)
                        || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 7 && k == 66) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 7 && k == 67) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 7 && k == 68) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 7 && k == 69) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 7 && k == 70) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 7 && k >=71&&k<=77) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 7 && k >= 78 && k <= 82)
                        || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 7 && k >= 83 && k <= 85)


                        || (i >= 0 && i <= colNum - 35 && j == (rowNum / materialNumber) + 8 && k == 0) || (i >= 0 && i <= colNum - 36 && j == (rowNum / materialNumber) + 8 && k == 1) || (i >= 0 && i <= colNum - 37 && j == (rowNum / materialNumber) + 8 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 8 && k == 4) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 8 && k >= 5 && k <= 6) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 8 && k == 7)
                        || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 8 && k == 8) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 8 && k == 9) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 8 && k == 10) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 8 && k == 11) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 8 && k == 12) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 8 && k == 13) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 8 && k == 14)
                        || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 8 && k == 15) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 8 && k == 16) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 8 && k >= 17&&k<=18) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 8 && k == 19) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 8 && k == 20) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 8 && k == 21) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 8 && k == 22)
                        || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 8 && k >=23&&k<=31) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 8 && k >= 32&&k<=33) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 8 && k == 34) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 8 && k == 35) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 8 && k == 36) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 8 && k == 37) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 8 && k == 38)
                        || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 8 && k == 39) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 8 && k >= 40 && k <= 44) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 8 && k >= 45 && k <= 46) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 8 && k >= 47 && k <= 48) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 8 && k >= 49 && k <= 52) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 8 && k >= 53 && k <= 54)
                        || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 8 && k >= 55 && k <= 58) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 8 && k >= 59 && k <= 60) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 8 && k == 61) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 8 && k == 62) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 8 && k == 63) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 8 && k == 64) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 8 && k == 65)
                        || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 8 && k == 66) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 8 && k == 67) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 8 && k == 68) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 8 && k == 69) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 8 && k == 70) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 8 && k == 71) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 8 && k >=72&&k<=82)
                        || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 8 && k >= 83 && k <= 85)





                        || (i >= 0 && i <= colNum - 38 && j == (rowNum / materialNumber) + 9 && k >= 0 && k <= 1) || (i >= 0 && i <= colNum - 39 && j == (rowNum / materialNumber) + 9 && k == 2) || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 9 && k == 3) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 9 && k >= 4 && k <= 5) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 9 && k == 6) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 9 && k == 7) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 9 && k == 8)
                        || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 9 && k == 9) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 9 && k == 10) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 9 && k == 11) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 9 && k == 12) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 9 && k == 13) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 9 && k == 14) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 9 && k >= 15&&k<=16)
                        || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 9 && k == 17) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 9 && k == 18) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 9 && k == 19) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 9 && k == 20) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 9 && k >= 21&&k<=22) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 9 && k >= 23 && k <= 26) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 9 && k >= 27 && k <= 28)
                        || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 9 && k >= 29 && k <= 31) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 9 && k == 32) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 9 && k == 33) || (i >= 0 && i <= colNum - 76&& j == (rowNum / materialNumber) + 9 && k == 34) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 9 && k == 35) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 9 && k == 36) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 9 && k == 37)
                        || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 9 && k == 38) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 9 && k == 39) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 9 && k == 40) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 9 && k >= 41 && k <= 42) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 9 && k >= 43&& k <= 44) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 9 && k >= 45 && k <= 46) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 9 && k >= 47 && k <= 54)
                        || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 9 && k >= 55 && k <= 59) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 9 && k == 60) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 9 && k == 61) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 9 && k == 62) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 9 && k == 63) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 9 && k == 64) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 9 && k == 65)
                        || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 9 && k == 66) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 9 && k == 67) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 9 && k == 68) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 9 && k == 69) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 9 && k == 70) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 9 && k == 71) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 9 && k == 72)
                        || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 9 && k >= 73 && k <= 81) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 9 && k >= 82 && k <= 85)



                        || (i >= 0 && i <= colNum - 40 && j == (rowNum / materialNumber) + 10 && k == 0) || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 10 && k == 1) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 10 && k >= 2 && k <= 3) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 10 && k == 4) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 10 && k == 5) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 10 && k == 6) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 10 && k == 7)
                        || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 10 && k == 8) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 10 && k == 9) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 10 && k == 10) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 10 && k == 11) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 10 && k == 12) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 10 && k >=13&&k<=14) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 10 && k == 15)
                        || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 10 && k == 16) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 10 && k == 17) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 10 && k == 18) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 10 && k == 19) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 10 && k >= 20 && k <= 21) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 10 && k >= 22 && k <= 28) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 10 && k >= 29 && k <= 31)
                        || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 10 && k == 32) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 10 && k == 33) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 10 && k == 34) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 10 && k == 35) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 10 && k == 36) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 10 && k == 37) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 10 && k == 38)
                        || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 10 && k == 39) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 10 && k >= 40 && k <= 42) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 10 && k >= 43 && k <= 44) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 10 && k >= 45 && k <= 47) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 10 && k >= 48 && k <= 55) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 10 && k >= 56 && k <= 58)
                        || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 10 && k >= 59 && k <= 60) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 10 && k == 61) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 10 && k == 62) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 10 && k == 63) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 10 && k == 64) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 10 && k == 65) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 10 && k == 66)
                        || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 10 && k == 67) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 10 && k == 68) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 10 && k == 69) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 10 && k == 70) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 10 && k == 71) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 10 && k == 72) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 10 && k >=73&&k<=74)
                        || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 10 && k >= 75 && k <= 80) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 10 && k >= 81 && k <= 83) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 10 && k >= 84 && k <= 85)


                        || (i >= 0 && i <= colNum - 41 && j == (rowNum / materialNumber) + 11 && k == 0) || (i >= 0 && i <= colNum - 42 && j == (rowNum / materialNumber) + 11 && k == 1) || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 11 && k == 2) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 11 && k == 3) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 11 && k == 4) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 11 && k == 5) || (i >= 0 && i <= colNum - 51 && j == (rowNum / materialNumber) + 11 && k == 6)
                        || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 11 && k == 7) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 11 && k == 8) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 11 && k == 9) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 11 && k >= 10&&k<=11) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 11 && k ==12) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 11 && k == 13)
                        || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 11 && k == 14) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 11 && k == 15) || ( i == colNum - 64 && j == (rowNum / materialNumber) + 11 && k == 15) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 11 && k == 16) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 11 && k == 17) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 11 && k == 18) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 11 && k == 19)
                        || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 11 && k >=20&&k<=27) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 11 && k >= 28 && k <= 30) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 11 && k == 31) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 11 && k == 32) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 11 && k == 33) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 11 && k == 34) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 11 && k == 35)
                        || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 11 && k == 36) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 11 && k == 37) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 11 && k >= 38 && k <= 42) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 11 && k ==43) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 11 && k >= 44 && k <= 45) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 11 && k >= 46 && k <= 48) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 11 && k >= 49 && k <= 59)
                        || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 11 && k >= 60 && k <= 61) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 11 && k == 62) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 11 && k == 63) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 11 && k == 64) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 11 && k == 65) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 11 && k == 66) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 11 && k == 67) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 11 && k == 68)
                        || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 11 && k == 69) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 11 && k == 70) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 11 && k == 71) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 11 && k == 72) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 11 && k == 73) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 11 && k == 74)
                        || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 11 && k >= 75 && k <= 77) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 11 && k >= 78 && k <= 79) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 11 && k >= 80 && k <= 82) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 11 && k >= 83 && k <= 84) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 11 && k ==85)


                        || (i >= 0 && i <= colNum - 43 && j == (rowNum / materialNumber) + 12 && k == 0) || (i >= 0 && i <= colNum - 44 && j == (rowNum / materialNumber) + 12 && k == 1) || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 12 && k == 2) || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 12 && k == 3) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 12 && k == 4) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 12 && k == 5) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 12 && k == 6)
                        || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 12 && k == 7) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 12 && k == 8) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 12 && k == 9) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 12 && k == 10) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 12 && k == 11) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 12 && k == 12) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 12 && k == 13)
                        || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 12 && k == 14) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 12 && k == 15) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 12 && k == 16) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 12 && k == 17) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 12 && k == 18) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 12 && k >=19&&k<=23) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 12 && k >= 24 && k <= 27)
                        || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 12 && k >= 28 && k <= 29) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 12 && k >= 30 && k <= 31) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 12 && k ==32) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 12 && k == 33) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 12 && k == 34) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 12 && k == 35) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 12 && k == 36)
                        || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 12 && k >= 44 && k <= 45) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 12 && k == 46) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 12 && k >= 47 && k <= 50) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 12 && k >= 51 && k <= 59) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 12 && k >= 60 && k <= 62) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 12 && k == 63) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 12 && k == 64)
                        || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 12 && k == 65) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 12 && k == 66) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 12 && k == 67) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 12 && k == 68) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 12 && k == 69) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 12 && k == 70) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 12 && k == 71)
                        || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 12 && k == 72) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 12 && k == 73) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 12 && k == 74) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 12 && k >= 75 && k <= 77) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 12 && k >= 78 && k <= 81) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 12 && k >= 82 && k <= 85)




                        || (i >= 0 && i <= colNum - 45 && j == (rowNum / materialNumber) + 13 && k == 0) || (i >= 0 && i <= colNum - 46 && j == (rowNum / materialNumber) + 13 && k == 1) || (i >= 0 && i <= colNum - 48 && j == (rowNum / materialNumber) + 13 && k == 2) || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 13 && k == 3) || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 13 && k == 4) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 13 && k == 5) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 13 && k == 6)
                        || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 13 && k == 7) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 13 && k == 8) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 13 && k == 9) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 13 && k == 10) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 13 && k == 11) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 13 && k == 12) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 13 && k == 13)
                        || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 13 && k == 14) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 13 && k == 15) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 13 && k == 16) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 13 && k == 17) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 13 && k >=18 && k<=21) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 13 && k >=22&&k<=24) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 13 && k >= 25 && k <= 27)
                        || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 13 && k >= 28 && k <= 29) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 13 && k ==30) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 13 && k == 31) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 13 && k == 32) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 13 && k == 33) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 13 && k == 34)
                        || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 13 && k >= 49 && k <= 51) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 13 && k >= 52&& k <= 57) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 13 && k >= 58 && k <= 63) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 13 && k == 64) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 13 && k == 65) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 13 && k == 66)
                        || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 13 && k == 67) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 13 && k == 68) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 13 && k == 69) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 13 && k == 70) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 13 && k == 71) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 13 && k == 72) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 13 && k == 73)
                        || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 13 && k == 74) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 13 && k == 75) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 13 && k >=76&&k<=78) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 13 && k >= 79 && k <= 81) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 13 && k >= 82 && k <= 85)




                        || (i >= 0 && i <= colNum - 47 && j == (rowNum / materialNumber) + 14 && k == 0) || (i >= 0 && i <= colNum - 49 && j == (rowNum / materialNumber) + 14 && k == 1) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 14 && k == 2) || (i >= 0 && i <= colNum - 54 && j == (rowNum / materialNumber) + 14 && k == 3) || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 14 && k == 4) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 14 && k == 5) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 14 && k == 6)
                        || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 14 && k == 7) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 14 && k == 8) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 14 && k == 9) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 14 && k == 10) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 14 && k == 11) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 14 && k == 12) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 14 && k == 13) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 14 && k == 14)
                        || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 14 && k == 15) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 14 && k == 16) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 14 && k >=17&&k<=20) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 14 && k == 21) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 14 && k >= 22 && k <= 28) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 14 && k >= 29 && k <= 30)
                        || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 14 && k == 31) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 14 && k == 32) || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 14 && k == 33) || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 14 && k >= 55 && k <= 62) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 14 && k >= 63 && k <= 64) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 14 && k == 65) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 14 && k == 66)
                        || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 14 && k == 67) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 14 && k == 68) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 14 && k == 69) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 14 && k == 70) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 14 && k == 71) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 14 && k == 72) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 14 && k == 73)
                        || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 14 && k == 74) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 14 && k == 75) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 14 && k == 76) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 14 && k >=77&&k<=80) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 14 && k >= 81 && k <= 84) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 14 && k ==85)



                        || (i >= 0 && i <= colNum - 50 && j == (rowNum / materialNumber) + 15 && k == 0) || (i >= 0 && i <= colNum - 52 && j == (rowNum / materialNumber) + 15 && k == 1) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 15 && k == 2) || (i >= 0 && i <= colNum - 57 && j == (rowNum / materialNumber) + 15 && k == 3) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 15 && k == 4) || (i >= 0 && i <= colNum - 60 && j == (rowNum / materialNumber) + 15 && k == 5) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 15 && k == 6)
                        || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 15 && k == 7) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 15 && k == 8) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 15 && k == 9) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 15 && k == 10) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 15 && k == 11) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 15 && k == 12) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 15 && k == 13)
                        || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 15 && k == 14) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 15 && k == 15) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 15 && k == 16) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 15 && k >=17&&k<=19) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 15 && k == 20) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 15 && k >= 21 && k <= 22) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 15 && k >= 23 && k <= 24)
                        || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 15 && k >= 25 && k <= 26) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 15 && k >= 27 && k <= 28) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 15 && k == 29) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 15 && k == 30) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 15 && k == 31)
                        || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 15 && k >= 64 && k <= 65) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 15 && k == 66) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 15 && k == 67) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 15 && k == 68) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 15 && k == 69) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 15 && k == 70) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 15 && k == 71)
                        || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 15 && k == 72) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 15 && k == 73) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 15 && k == 74) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 15 && k == 75) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 15 && k == 76) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 15 && k == 77) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 15 && k >= 78&&k<=85)



                        || (i >= 0 && i <= colNum - 53 && j == (rowNum / materialNumber) + 16 && k == 0) || (i >= 0 && i <= colNum - 55 && j == (rowNum / materialNumber) + 16 && k == 1) || (i >= 0 && i <= colNum - 58 && j == (rowNum / materialNumber) + 16 && k == 2) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 16 && k == 3) || (i >= 0 && i <= colNum - 62 && j == (rowNum / materialNumber) + 16 && k == 4) || (i >= 0 && i <= colNum - 65 && j == (rowNum / materialNumber) + 16 && k == 5) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 16 && k == 6)
                        || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 16 && k == 7) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 16 && k == 8) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 16 && k == 9) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 16 && k == 10) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 16 && k == 11) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 16 && k == 12) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 16 && k == 13)
                        || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 16 && k == 14) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 16 && k == 15) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 16 && k == 16) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 16 && k == 17) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 16 && k >=18&&k<=20) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 16 && k >= 21 && k <= 22)
                        || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 16 && k == 23) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 16 && k >= 24 && k <= 26) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 16 && k == 27) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 16 && k == 28) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 16 && k == 29)
                        || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 16 && k == 67) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 16 && k == 68) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 16 && k == 69) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 16 && k == 70) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 16 && k == 71) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 16 && k == 72) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 16 && k == 73)
                        || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 16 && k == 74) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 16 && k == 75) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 16 && k == 76) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 16 && k >=77&&k<=78) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 16 && k >= 79 && k <= 84) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 16 && k == 85)




                        || (i >= 0 && i <= colNum - 56 && j == (rowNum / materialNumber) + 17 && k == 0) || (i >= 0 && i <= colNum - 59 && j == (rowNum / materialNumber) + 17 && k == 1) || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 17 && k == 2) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 17 && k == 3) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 17 && k == 4) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 17 && k == 5) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 17 && k == 6)
                        || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 17 && k == 7) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 17 && k == 8) || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 17 && k == 9) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 17 && k == 10) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 17 && k == 11) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 17 && k == 12) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 17 && k == 13)
                        || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 17 && k >= 14 && k<=15) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 17 && k >= 16 && k <= 17) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 17 && k >= 18 && k <= 21) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 17 && k >= 22 && k <= 23) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 17 && k >= 24 && k <= 25) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 17 && k ==26) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 17 && k == 27)
                        || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 17 && k == 69) || (i >= 0 && i <= colNum - 89 && j == (rowNum / materialNumber) + 17 && k == 70) || (i >= 0 && i <= colNum - 87 && j == (rowNum / materialNumber) + 17 && k == 71) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 17 && k == 72) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 17 && k == 73) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 17 && k == 74) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 17 && k == 75)
                        || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 17 && k == 76) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 17 && k >=77&&k<=78) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 17 && k >= 79 && k <= 85)


                        || (i >= 0 && i <= colNum - 61 && j == (rowNum / materialNumber) + 18 && k == 0) || (i >= 0 && i <= colNum - 63 && j == (rowNum / materialNumber) + 18 && k == 1) || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 18 && k == 2) || (i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 18 && k == 3) || (i >= 0 && i <= colNum - 69 && j == (rowNum / materialNumber) + 18 && k == 4) || (i >= 0 && i <= colNum - 71 && j == (rowNum / materialNumber) + 18 && k == 5) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 18 && k == 6)
                        || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 18 && k == 7) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 18 && k == 8) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 18 && k == 9) || (i >= 2 && i <= colNum - 85 && j == (rowNum / materialNumber) + 18 && k == 10) || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 18 && k >=19&&k<=24)
                        || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 18 && k == 71) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 18 && k == 72) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 18 && k == 73) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 18 && k == 74) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 18 && k == 75) || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 18 && k == 76) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 18 && k == 77)
                        || (i >= 0 && i <= colNum - 77 && j == (rowNum / materialNumber) + 18 && k >=78&&k<=79) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 18 && k >= 80 && k <= 81) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 18 && k ==82) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 18 && k >= 83 && k <= 85)


                        || (i >= 0 && i <= colNum - 64 && j == (rowNum / materialNumber) + 19 && k == 0)||(i >= 0 && i <= colNum - 66 && j == (rowNum / materialNumber) + 19 && k == 1) || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 19 && k == 2) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 19 && k == 3) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 19 && k == 4) || (i >= 0 && i <= colNum - 75 && j == (rowNum / materialNumber) + 19 && k == 5) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 19 && k == 6)
                        || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 19 && k == 7) || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 19 && k == 72) || (i >= 0 && i <= colNum - 90 && j == (rowNum / materialNumber) + 19 && k == 73) || (i >= 0 && i <= colNum - 86 && j == (rowNum / materialNumber) + 19 && k == 74) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 19 && k == 75) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 19 && k == 76) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 19 && k >= 77&&k<=78)
                        || (i >= 0 && i <= colNum - 79 && j == (rowNum / materialNumber) + 19 && k == 79) || (i >= 0 && i <= colNum - 78 && j == (rowNum / materialNumber) + 19 && k >=80&&k<=85)


                        || (i >= 0 && i <= colNum - 67 && j == (rowNum / materialNumber) + 20 && k == 0) || (i >= 0 && i <= colNum - 68 && j == (rowNum / materialNumber) + 20 && k == 1) || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 20 && k == 2) || (i >= 0 && i <= colNum - 73 && j == (rowNum / materialNumber) + 20 && k == 3) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 20 && k == 4) || (i >= 2 && i <= colNum - 79 && j == (rowNum / materialNumber) + 20 && k == 5)
                        || (i >= 0 && i <= colNum - 91 && j == (rowNum / materialNumber) + 20 && k == 74) || (i >= 0 && i <= colNum - 88 && j == (rowNum / materialNumber) + 20 && k == 75) || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 20 && k == 76) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 20 && k == 77) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 20 && k >=78&&k<=79) || (i >= 0 && i <= colNum - 81 && j == (rowNum / materialNumber) + 20 && k == 80) || (i >= 0 && i <= colNum - 80 && j == (rowNum / materialNumber) + 20 && k >=81&&k<=85)


                        || (i >= 0 && i <= colNum - 70 && j == (rowNum / materialNumber) + 21 && k == 0) || (i >= 0 && i <= colNum - 72 && j == (rowNum / materialNumber) + 21 && k == 1) || (i >= 0 && i <= colNum - 74 && j == (rowNum / materialNumber) + 21 && k == 2) || (i >= 0 && i <= colNum - 76 && j == (rowNum / materialNumber) + 21 && k == 3) || (i >= colNum - 85 && i <= colNum - 81 && j == (rowNum / materialNumber) + 21 && k == 4)
                        || (i >= 0 && i <= colNum - 85 && j == (rowNum / materialNumber) + 21 && k == 78) || (i >= 0 && i <= colNum - 84 && j == (rowNum / materialNumber) + 21 && k == 79) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 21 && k >=80&&k<=81) || (i >= 0 && i <= colNum - 82 && j == (rowNum / materialNumber) + 21 && k >= 82 && k <= 84) || (i >= 0 && i <= colNum - 83 && j == (rowNum / materialNumber) + 21 && k ==85)

                        || (i >= colNum-90 && i <= colNum - 73 && j == (rowNum / materialNumber) + 22 && k == 0) || (i >= colNum-88 && i <= colNum - 75 && j == (rowNum / materialNumber) + 22 && k == 1) || (i >= colNum-87 && i <= colNum - 77 && j == (rowNum / materialNumber) + 22 && k == 2)

                        || (i >= colNum-83 && i <= colNum - 80 && j == (rowNum / materialNumber) + 23 && k == 0)
                        )
                    {
                        pos.x = i * interval;
                        pos.y = j * interval;
                        pos.z = k * interval;
                        GameObject go = Instantiate(gridPrefab, pos, Quaternion.identity, transform);//克隆目标物体
                        go.name = "grid(" + i.ToString() + ',' + j.ToString() + ',' + k.ToString() + ')';
                        grids[i, j, k] = go.GetComponent<SingleGrid>();// 向每个三维坐标 注入 实例化网格 go     go由game object类型转化为 singlegrid类型
                        grids[i, j, k].AddGridEnergy(gridInitEnergy);//每个网格注入能量
                        grids[i, j, k].x = i;
                        grids[i, j, k].y = j;
                        grids[i, j, k].z = k;
                    }

                    else
                    {
                        pos.x = i * interval;
                        pos.y = j * interval;
                        pos.z = k * interval;
                        GameObject go = Instantiate(gridPrefab, pos, Quaternion.identity, transform);
                        go.name = "grid(" + i.ToString() + ',' + j.ToString() + ',' + k.ToString() + ')';
                        grids[i, j, k] = go.GetComponent<SingleGrid>();
                        grids[i, j, k].AddGridEnergy(gridMaxEnergy);
                        grids[i, j, k].x = i;
                        grids[i, j, k].y = j;
                        grids[i, j, k].z = k;
                    }



                }
    }

    
    //GetGrid
    public SingleGrid GetGridAroundV26(SingleGrid origon, GridsAroundV26 direction)//获取周围网格26
    {
        SingleGrid targetGrid = null;
        int origonX = origon.x;
        int origonY = origon.y;
        int origonZ = origon.z;
        switch(direction)
        {
            case GridsAroundV26.LEFTUP:
                {
                    if (origonX - 1 >= 0 && origonY + 1 < rowNum)
                        targetGrid = grids[origonX - 1 ,origonY+1,origonZ];
                    break;
                }
            case GridsAroundV26.LEFT:
                {
                    if (origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY , origonZ];
                    break;
                }
            case GridsAroundV26.LEFTDOWN:
                {
                    if (origonX - 1 >= 0 && origonY -1>=0)
                        targetGrid = grids[origonX - 1, origonY - 1, origonZ];
                    break;
                }
            case GridsAroundV26.UP:
                {
                    if (origonY + 1 < rowNum)
                        targetGrid = grids[origonX , origonY + 1, origonZ];
                    break;
                }
            case GridsAroundV26.DOWN:
                {
                    if (origonY -1 >=0)
                        targetGrid = grids[origonX , origonY - 1, origonZ];
                    break;
                }
            case GridsAroundV26.RIGHTUP:
                {
                    if (origonX +1<colNum && origonY + 1 < rowNum)
                        targetGrid = grids[origonX +1, origonY + 1, origonZ];
                    break;
                }
            case GridsAroundV26.RIGHT:
                {
                    if (origonX +1<colNum)
                        targetGrid = grids[origonX +1, origonY, origonZ];
                    break;
                }
            case GridsAroundV26.RIGHTDOWN:
                {
                    if (origonX +1<colNum && origonY -1 >=0)
                        targetGrid = grids[origonX+ 1, origonY- 1, origonZ];
                    break;
                }
            case GridsAroundV26.FRONT:
                {
                    if (origonZ + 1 < highNum)
                        targetGrid = grids[origonX, origonY, origonZ + 1];
                    break;
                }
            
            case GridsAroundV26.FRONTLEFTUP:
                {
                    if (origonZ + 1 < highNum && origonY + 1 < rowNum && origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY + 1, origonZ + 1];
                    break;
                }
            case GridsAroundV26.FRONTLEFT:
                {
                    if (origonZ + 1 < highNum && origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ + 1];
                    break;
                }

            case GridsAroundV26.FRONTLEFTDOWN:
                {
                    if (origonZ + 1 < highNum && origonX - 1 >= 0 && origonY - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY - 1, origonZ + 1];
                    break;
                }
            case GridsAroundV26.FRONTUP:
                {
                    if (origonZ + 1 < highNum && origonY + 1 < rowNum)
                        targetGrid = grids[origonX, origonY + 1, origonZ + 1];
                    break;
                }
            case GridsAroundV26.FRONTDOWN:
                {
                    if (origonZ + 1 < highNum && origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ + 1];
                    break;
                }
            case GridsAroundV26.FRONTRIGHTUP:
                {
                    if (origonZ + 1 < highNum && origonX + 1 < colNum && origonY + 1 < rowNum)
                        targetGrid = grids[origonX + 1, origonY + 1, origonZ + 1];
                    break;
                }

            case GridsAroundV26.FRONTRIGHT:
                {
                    if (origonZ + 1 < highNum && origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ + 1];
                    break;
                }
            case GridsAroundV26.FRONTRIGHTDOWN:
                {
                    if (origonZ + 1 < highNum && origonX + 1 < colNum && origonY - 1 >= 0)
                        targetGrid = grids[origonX + 1, origonY - 1, origonZ + 1];
                    break;
                }
            
            
            case GridsAroundV26.BEHIND:
                {
                    if (origonZ-1>=0)
                        targetGrid = grids[origonX, origonY , origonZ-1];
                    break;
                }
            
            case GridsAroundV26.BEHINDLEFTUP:
                {
                    if (origonZ - 1 >= 0 && origonY + 1 < rowNum && origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY + 1, origonZ - 1];
                    break;
                }
            case GridsAroundV26.BEHINDLEFT:
                {
                    if (origonZ - 1 >= 0 && origonX -1>=0)
                        targetGrid = grids[origonX-1, origonY, origonZ - 1];
                    break;
                }
            case GridsAroundV26.BEHINDLEFTDOWN:
                {
                    if (origonZ - 1 >= 0 && origonY-1>=0 && origonX-1>=0)
                        targetGrid = grids[origonX-1, origonY-1, origonZ - 1];
                    break;
                }
            case GridsAroundV26.BEHINDUP:
                {
                    if (origonZ - 1 >= 0 && origonY + 1 < rowNum)
                        targetGrid = grids[origonX, origonY + 1, origonZ - 1];
                    break;
                }
            case GridsAroundV26.BEHINDDOWN:
                {
                    if (origonZ - 1 >= 0 && origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ - 1];
                    break;
                }
            case GridsAroundV26.BEHINDRIGHTUP:
                {
                    if (origonZ - 1 >= 0 && origonY + 1 < rowNum && origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY + 1, origonZ - 1];
                    break;
                }

            case GridsAroundV26.BEHINDRIGHT:
                {
                    if (origonZ - 1 >= 0  && origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY , origonZ - 1];
                    break;
                }
            case GridsAroundV26.BEHINDRIGHTDOWN:
                {
                    if (origonZ - 1 >= 0 && origonY - 1 >= 0 && origonX + 1 <colNum)
                        targetGrid = grids[origonX + 1, origonY - 1, origonZ - 1];
                    break;
                }
            
            
            
        }
        return targetGrid;
    }

    public SingleGrid GetGridAroundV6(SingleGrid origon, GridsAroundV6 direction)//获取周围网格V6
    {
        SingleGrid targetGrid = null;
        int origonX = origon.x;
        int origonY = origon.y;
        int origonZ = origon.z;
        switch (direction)
        {
            
            case GridsAroundV6.LEFT:
                {
                    if (origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ];
                    break;
                }
            
            case GridsAroundV6.UP:
                {
                    if (origonY + 1 < rowNum)
                        targetGrid = grids[origonX, origonY + 1, origonZ];
                    break;
                }
            case GridsAroundV6.DOWN:
                {
                    if (origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ];
                    break;
                }
            
            case GridsAroundV6.RIGHT:
                {
                    if (origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ];
                    break;
                }
            
            case GridsAroundV6.BEHIND:
                {
                    if (origonZ - 1 >= 0)
                        targetGrid = grids[origonX, origonY, origonZ - 1];
                    break;
                }
            
            case GridsAroundV6.FRONT:
                {
                    if (origonZ + 1 < highNum)
                        targetGrid = grids[origonX, origonY, origonZ + 1];
                    break;
                }
            
        }
        return targetGrid;
    }

    public SingleGrid GetGridAroundV5(SingleGrid origon, GridsAroundV5 direction)//获取周围网格V6
    {
        SingleGrid targetGrid = null;
        int origonX = origon.x;
        int origonY = origon.y;
        int origonZ = origon.z;
        switch (direction)
        {

         
            case GridsAroundV5.DOWN:
                {
                    if (origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ];
                    break;
                }

            case GridsAroundV5.LEFT:
                {
                    if (origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ];
                    break;
                }


            case GridsAroundV5.RIGHT:
                {
                    if (origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ];
                    break;
                }

            case GridsAroundV5.FRONT:
                {
                    if (origonZ + 1 < highNum)
                        targetGrid = grids[origonX, origonY, origonZ + 1];
                    break;
                }

            case GridsAroundV5.BEHIND:
                {
                    if (origonZ - 1 >= 0)
                        targetGrid = grids[origonX, origonY, origonZ - 1];
                    break;
                }

        }
        return targetGrid;
    }

    public SingleGrid GetGridAroundV8(SingleGrid origon, GridsAroundV8 direction)//获取周围网格V8
    {
        SingleGrid targetGrid = null;
        int origonX = origon.x;
        int origonY = origon.y;
        int origonZ = origon.z;
        switch (direction)
        {
            
            case GridsAroundV8.LEFT:
                {
                    if (origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ];
                    break;
                }
            
            
            case GridsAroundV8.RIGHT:
                {
                    if (origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ];
                    break;
                }
            
            case GridsAroundV8.BEHIND:
                {
                    if (origonZ - 1 >= 0)
                        targetGrid = grids[origonX, origonY, origonZ - 1];
                    break;
                }
            
            case GridsAroundV8.BEHINDLEFT:
                {
                    if (origonZ - 1 >= 0 && origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ - 1];
                    break;
                }
            
            case GridsAroundV8.BEHINDRIGHT:
                {
                    if (origonZ - 1 >= 0 && origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ - 1];
                    break;
                }
            
            
            case GridsAroundV8.FRONT:
                {
                    if (origonZ + 1 < highNum)
                        targetGrid = grids[origonX, origonY, origonZ + 1];
                    break;
                }
            
            case GridsAroundV8.FRONTLEFT:
                {
                    if (origonZ + 1 < highNum && origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ + 1];
                    break;
                }
            
            case GridsAroundV8.FRONTRIGHT:
                {
                    if (origonZ + 1 < highNum && origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ + 1];
                    break;
                }
            
        }
        return targetGrid;
    }
    public SingleGrid GetGridAroundV14(SingleGrid origon, GridsAroundV14 direction)//获取周围网格26
    {
        SingleGrid targetGrid = null;
        int origonX = origon.x;
        int origonY = origon.y;
        int origonZ = origon.z;
        switch (direction)
        {
            case GridsAroundV14.LEFTUP:
                {
                    if (origonX - 1 >= 0 && origonY + 1 < rowNum)
                        targetGrid = grids[origonX - 1, origonY + 1, origonZ];
                    break;
                }
            case GridsAroundV14.LEFT:
                {
                    if (origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ];
                    break;
                }
            case GridsAroundV14.LEFTDOWN:
                {
                    if (origonX - 1 >= 0 && origonY - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY - 1, origonZ];
                    break;
                }
            case GridsAroundV14.UP:
                {
                    if (origonY + 1 < rowNum)
                        targetGrid = grids[origonX, origonY + 1, origonZ];
                    break;
                }
            case GridsAroundV14.DOWN:
                {
                    if (origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ];
                    break;
                }
            case GridsAroundV14.RIGHTUP:
                {
                    if (origonX + 1 < colNum && origonY + 1 < rowNum)
                        targetGrid = grids[origonX + 1, origonY + 1, origonZ];
                    break;
                }
            case GridsAroundV14.RIGHT:
                {
                    if (origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ];
                    break;
                }
            case GridsAroundV14.RIGHTDOWN:
                {
                    if (origonX + 1 < colNum && origonY - 1 >= 0)
                        targetGrid = grids[origonX + 1, origonY - 1, origonZ];
                    break;
                }
            case GridsAroundV14.BEHIND:
                {
                    if (origonZ - 1 >= 0)
                        targetGrid = grids[origonX, origonY, origonZ - 1];
                    break;
                }
            case GridsAroundV14.BEHINDDOWN:
                {
                    if (origonZ - 1 >= 0 && origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ - 1];
                    break;
                }
            case GridsAroundV14.BEHINDUP:
                {
                    if (origonZ - 1 >= 0 && origonY + 1 < rowNum)
                        targetGrid = grids[origonX, origonY + 1, origonZ - 1];
                    break;
                }
            case GridsAroundV14.FRONT:
                {
                    if (origonZ + 1 < highNum)
                        targetGrid = grids[origonX, origonY, origonZ + 1];
                    break;
                }
            case GridsAroundV14.FRONTDOWN:
                {
                    if (origonZ + 1 < highNum && origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ + 1];
                    break;
                }
            case GridsAroundV14.FRONTUP:
                {
                    if (origonZ + 1 < highNum && origonY + 1 < rowNum)
                        targetGrid = grids[origonX, origonY + 1, origonZ + 1];
                    break;
                }
        }
        return targetGrid;
    }

    public SingleGrid GetGridAroundV9(SingleGrid origon, GridsAroundV9 direction)//获取周围网格26
    {
        SingleGrid targetGrid = null;
        int origonX = origon.x;
        int origonY = origon.y;
        int origonZ = origon.z;
        switch (direction)
        {
            
            case GridsAroundV9.LEFT:
                {
                    if (origonX - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY, origonZ];
                    break;
                }
            case GridsAroundV9.LEFTDOWN:
                {
                    if (origonX - 1 >= 0 && origonY - 1 >= 0)
                        targetGrid = grids[origonX - 1, origonY - 1, origonZ];
                    break;
                }
            
            case GridsAroundV9.DOWN:
                {
                    if (origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ];
                    break;
                }
            
            case GridsAroundV9.RIGHT:
                {
                    if (origonX + 1 < colNum)
                        targetGrid = grids[origonX + 1, origonY, origonZ];
                    break;
                }
            case GridsAroundV9.RIGHTDOWN:
                {
                    if (origonX + 1 < colNum && origonY - 1 >= 0)
                        targetGrid = grids[origonX + 1, origonY - 1, origonZ];
                    break;
                }

            case GridsAroundV9.FRONT:
                {
                    if (origonZ + 1 < highNum)
                        targetGrid = grids[origonX, origonY, origonZ + 1];
                    break;
                }
            case GridsAroundV9.FRONTDOWN:
                {
                    if (origonZ + 1 < highNum && origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ + 1];
                    break;
                }
            case GridsAroundV9.BEHIND:
                {
                    if (origonZ - 1 >= 0)
                        targetGrid = grids[origonX, origonY, origonZ - 1];
                    break;
                }
            case GridsAroundV9.BEHINDDOWN:
                {
                    if (origonZ - 1 >= 0 && origonY - 1 >= 0)
                        targetGrid = grids[origonX, origonY - 1, origonZ - 1];
                    break;
                }
            
            
            
        }
        return targetGrid;
    }

    public SingleGrid RandomGetGridWithoutCellAroundGridV6(SingleGrid origon)//随机获取周围没有细胞的 能量值为0的网格
    {
        //随机获取有能量的网格

        List<SingleGrid> array = new List<SingleGrid>();
        for (int i = 0; i < 6; i++)
        {
            SingleGrid randomGrid = GetGridAroundV6(origon, (GridsAroundV6)i);
            if (allPrefab.IndexOf(randomGrid) >= 0)//连接到DLA主管道
                return null;

            if (randomGrid != null && randomGrid.HasCell() && randomGrid.gridEnergy == 100 )//不是null 说明在网格cube里 而不是在外部环境 
                array.Add(randomGrid);
                
        }
        int number = array.Count;
        if (number != 0)
        {
            int random = UnityEngine.Random.Range(0, number);
            //return InverseGrid(array[random]);
            return array[random];
        }
        else
            return null;
    }

    public SingleGrid RandomGetGridWithoutCellAroundGridV14(SingleGrid origon)//随机获取周围没有细胞的 能量值为0的网格
    {
        
        int x = initialAgent.x;
        int z = initialAgent.z;

        List<SingleGrid> array = new List<SingleGrid>();
        for (int i = 0; i < 14; i++)
        {
            SingleGrid randomGrid = GetGridAroundV14(origon, (GridsAroundV14)i);

   

            if (randomGrid != null && !randomGrid.HasCell() && randomGrid.gridEnergy <=0)//不是null 说明在网格cube里 而不是在外部环境 
                  array.Add(randomGrid);

        }
        int number = array.Count;
        if (number != 0)
        {
            int random = UnityEngine.Random.Range(0, number);
            return array[random];
        }
        else
            return null;//如果没有可移动目标网格 就在原位置待机一轮
    }

    public SingleGrid RandomGetGridWithoutCellAroundGridV9(SingleGrid origon)//随机获取地下能量值为50的网格V5
    {

        //int x = initialAgent.x;
        //int z = initialAgent.z;

        List<SingleGrid> array = new List<SingleGrid>();
        for (int i = 0; i < 9; i++)
        {
            SingleGrid randomGrid = GetGridAroundV9(origon, (GridsAroundV9)i);

            //if (randomGrid != null && !randomGrid.HasCell() && RandomGridV6HasAtLeastOneCellWithMaterial(randomGrid) && randomGrid.gridEnergy <50
            //    && randomGrid.x <= (x+7) && randomGrid.x >= (x-6) && randomGrid.z <= (z+18) && randomGrid.z >= (z-13))//不是null 说明在网格cube里 而不是在外部环境 

            if (randomGrid != null && !randomGrid.HasCell() && randomGrid.gridEnergy == 50)//不是null 说明在网格cube里 而不是在外部环境 
                array.Add(randomGrid);

        }
        int number = array.Count;
        if (number != 0)
        {
            int random = UnityEngine.Random.Range(0, number);
            return array[random];
        }
        else
            return null;//如果没有可移动目标网格 就在原位置待机一轮
    }

    public bool RandomGridV6HasAtLeastOneCellWithMaterial(SingleGrid randomGrid)//判断随机网格的V6的邻居里是否至少有一个邻居含有材料
    {

        for (int i = 0; i < 6; i++)
        {
            SingleGrid randomNeighbourGrid = GetGridAroundV6(randomGrid, (GridsAroundV6)i);
            if ((randomNeighbourGrid != null && !randomNeighbourGrid.HasCell() && randomNeighbourGrid.gridEnergy == 50) || (randomNeighbourGrid != null && randomNeighbourGrid.HasCell() && randomNeighbourGrid.GetCell().currentEnergy == 100))
                return true;
        }

        return false;

    }



    //Underground Part
    public SingleGrid BottomGrid(SingleGrid randomGrid)//agent 所在网格的底层网格 底层网格为含有材料的将要拾取的目标网格
    { 
        SingleGrid bottomGrid = GetGridAroundV6(randomGrid, GridsAroundV6.DOWN);
        
        return bottomGrid;
    }

    public SingleGrid NumBottomGrid(SingleGrid randomGrid,int num)//获取地下某个具体网格
    {
        

        for (int i = 0; i < num; i++)
        {
            randomGrid = BottomGrid(randomGrid);
   
        }
        return randomGrid;



    }

    public void BottomGridReduceEnergy(SingleGrid randomGrid,int num)//逐一减去地下网格的能量  每个减去的加入 list
    {
        for (int i = 0; i < num; i++)
        {
            randomGrid = BottomGrid(randomGrid);
            //UndergroundGridReduced.Add(randomGrid);//把地下每个减去的网格存入list中 做邻居规则运算
            randomGrid.ReduceEnergy(50);
            randomGrid.SpawnCell(90);

        }

    }

    public void BottomGridReducedList(SingleGrid randomGrid, int num)//逐一减去地下网格的能量  每个减去的加入 list
    {
        for (int i = 0; i < num; i++)
        {
            randomGrid = BottomGrid(randomGrid);
            UndergroundGridReduced.Add(randomGrid);//把地下每个减去的网格存入list中 做邻居规则运算
            randomGrid.ReduceEnergy(49);
            randomGrid.SpawnCell(90);

        }

    }
    public void CubicGridReduce(SingleGrid randomGrid,int num)//地下巢穴
    {

        for (int i = randomGrid.x-num; i < randomGrid.x+num+1; i++)
            for (int j = randomGrid.y - num; j < randomGrid.y+num+1; j++)
                for (int k = randomGrid.z-num; k < randomGrid.z+num+1; k++)
                {
                    if(grids[i, j, k] != null&& grids[i, j, k].gridEnergy==50)
                    {
                        grids[i, j, k].ReduceEnergy(50);
                        if (!grids[i, j, k].HasCell())
                            grids[i, j, k].SpawnCell(90);
                    }
                        

                }
    }

    public void UndergroundNestGridReduce(SingleGrid randomGrid)//地下巢穴  foreach 查找list里每个初始空grid 开始邻居规则运算
    {

        
        //V5邻居规则运算
        SingleGrid randomGridV9 =RandomGetGridWithoutCellAroundGridV9(randomGrid);//获取V5地下周边的网格
           
        if (randomGrid == null) // corner condition 
                return;

        if (!NewCellNeighborConditionsV9(randomGridV9))
                return;
        if (randomGridV9.y < 1 || randomGridV9.x < 1 || randomGridV9.x > colNum || randomGridV9.z < 1 || randomGridV9.z > highNum)
            return;
        randomGridV9.ReduceEnergy(49);
        randomGridV9.SpawnCell(90);
        reduceGrid++;
        //UndergroundGridReduced.Add(randomGridV9);
        //第二轮开始 foreach 查找所有gridenergy=0 筛选新减去的grid 存储到list里
            
        
    }
    public void AddGridReducedToList()
    {
        foreach (SingleGrid grid in grids)
        {
            if (grid.gridEnergy == 1)
            {
                if (UndergroundGridReduced.IndexOf(grid) >= 0)
                {
                    continue;
                }
                else
                {

                    UndergroundGridReduced.Add(grid);
                   
                }
            }
            else
                continue;
        }
    }
    public bool NewCellNeighborConditionsV9(SingleGrid g)//地下巢穴 邻居规则
    {
        for (int i = 0; i < 14; i++)
        {
            SingleGrid around = GetGridAroundV14(g, (GridsAroundV14)i);//遍历周边14个网格
            if (around == null)//corner condition
                continue;
            int aroundCellNum = around.gridEnergy == 1 ? 1 : 0;
            aroundCellNum += CellNumberAroundGridV9(around);//检查邻居的26个邻居
            if (aroundCellNum > overcrowdedNum)
                return false;
        }
        return true;
    }

    public int CellNumberAroundGridV9(SingleGrid grid)//计算细胞的邻居数量，总共可以再其周边26个方向生长 地下邻居规则
    {
        int num = 0;
        for (int i = 0; i < 14; i++)
        {
            SingleGrid around = GetGridAroundV14(grid, (GridsAroundV14)i);
            if (around != null && around.gridEnergy == 1)// 周边14个每个网格 around1、2、3、4、5......26
                num++;
        }
        return num;
    }




    //AboveGround Part
    public SingleGrid UpGrid(SingleGrid randomGrid)//agent 所在网格的底层网格 底层网格为含有材料的将要拾取的目标网格
    {
        SingleGrid upGrid = GetGridAroundV6(randomGrid, GridsAroundV6.UP);

        return upGrid;
    }

    public SingleGrid NumUpGrid(SingleGrid randomGrid, int num)//获取地下某个具体网格
    {


        for (int i = 0; i < num; i++)
        {
            randomGrid = UpGrid(randomGrid);

        }
        return randomGrid;



    }
    public int CountV8FullNeighbour(SingleGrid bottomGrid)//计算agent 底层细胞的V8含有材料的网格数量
    {
        int numberOfFullNeighbour = 0;
        for (int i = 0; i < 8; i++)
        {
            SingleGrid randomGridV8 = GetGridAroundV8(bottomGrid, (GridsAroundV8)i);
            if (randomGridV8 != null && !randomGridV8.HasCell() && randomGridV8.gridEnergy == 50)
                numberOfFullNeighbour=numberOfFullNeighbour+1;
        }
        return numberOfFullNeighbour;
    }

    public int CountV26FullNeighbour(SingleGrid randomGrid)//计算agent V26含有材料的网格数量
    {
        int numberOfFullNeighbour = 0;
        for (int i = 0; i < 26; i++)
        {
            SingleGrid randomGridV26 = GetGridAroundV26(randomGrid, (GridsAroundV26)i);
            if (randomGridV26 != null && !randomGridV26.HasCell() && randomGridV26.gridEnergy == 50)
                numberOfFullNeighbour +=1;
        }
        return numberOfFullNeighbour;
    }

    public bool DropProbability(SingleGrid randomGrid)//是否掉落
    {
        int numberOfFullNeighbour = CountV26FullNeighbour(randomGrid);
        double pDrop=0;
        if(numberOfFullNeighbour == 0)
        {
            //Mathf.Exp(10);

            pDrop= Mathf.Pow(10,-4);//10的5次方
            //return pDrop;

        }

        else
        {
            double drop1 = Mathf.Pow(10, -3);
            double amplifDrop = 0.036f;
            
            //drop1 + amplifDrop * (n - 1) * exp(-time - latestDropTime) * evap
            pDrop= drop1 + amplifDrop * (numberOfFullNeighbour - 1) * Mathf.Exp((-currentStep - latestDropAge) *(float) evap);
            
            //return pDrop;
        }

        double random = GetRandomNumber(0, 1,15);
        if (pDrop < random)
            return true;
        else
            return false;
    }

    public static double GetRandomNumber(double min, double max, int Len = 15)   //Len小数点保留位数
    {
        System.Random random = new System.Random();
        return Math.Round(random.NextDouble() * (max - min) + min, Len);
    }

    public bool PickUpProbability(SingleGrid randomGrid)//是否捡起
    {
        SingleGrid bottomGrid = BottomGrid(randomGrid);
        int numberOfFullNeighbour = CountV8FullNeighbour(bottomGrid);
        double pPick = 0;

        if (numberOfFullNeighbour == 0)
            pPick = Mathf.Pow(10, -2);
        else if (numberOfFullNeighbour == 8)
            pPick = Mathf.Pow(10, -2) / 100;
        else
            pPick = Mathf.Pow(10, -2) / (1*numberOfFullNeighbour);



        double random= GetRandomNumber(0, 1, 15);
        if ( pPick < random)
            return true;
        else
            return false;
        
    }

    public int CellNumberAroundGrid(SingleGrid grid)//计算细胞的邻居数量，总共可以再其周边26个方向生长
    {
        int num = 0;
        for (int i = 0; i < 14; i++)
        {
            SingleGrid around = GetGridAroundV14(grid, (GridsAroundV14)i);
            if (around != null && around.HasCell())// 周边14个每个网格 around1、2、3、4、5......26
                num++;
        }
        return num;
    }

    public bool NewCellNeighborConditions(SingleGrid g)//邻居规则
    {
        for (int i = 0; i < 14; i++)
        {
            SingleGrid around = GetGridAroundV14(g, (GridsAroundV14)i);//遍历周边26个网格
            if (around == null)//corner condition
                continue;
            int aroundCellNum = around.HasCell() ? 1 : 0;
            aroundCellNum += CellNumberAroundGrid(around);//检查邻居的26个邻居
            if (aroundCellNum > overcrowdedNumUp)
                return false;
        }
        return true;
    }
    public void AddCellSpawnedToList()
    {
        foreach (SingleGrid grid in grids)
        {

            if (grid.HasCell() && grid.GetCell().currentEnergy == 100)
            {

                if (AboveGroundCellSpawned.IndexOf(grid) >= 0)
                    continue;
                else
                    AboveGroundCellSpawned.Add(grid);

            }
            else
                continue;


        }
    }
    public void Split(SingleCell mother,float subCellEnergy)//地上结构的生成规则  初始母细胞为initial cells 获得DLA移动方向
    {
        SingleGrid randomGrid = RandomGetGridWithoutCellAroundGridV14(mother.GetGrid());//以初始母细胞所在的网格周边6个邻居寻找随机分裂的网格
        if (randomGrid == null) // corner condition 
            return;

        //竖向孔洞
        if (randomGrid.x == initialAgent.x && randomGrid.z == initialAgent.z)
            return;

        if (allPrefab.IndexOf(randomGrid) >= 0)
            return;
        //控制水平生长
        //if (cellNum < maxmount * 2 / 10)
        //{
        //    if (randomGrid.y > initialAgent.y + 5)//屋顶用
        //        return;
        //}
        //else if (cellNum < maxmount * 3 / 10)
        //{
        //    if (randomGrid.y > initialAgent.y + 4)//屋顶用
        //        return;
        //}
        //else if (cellNum < maxmount * 4 / 10)
        //{
        //    if (randomGrid.y > initialAgent.y + 3)//屋顶用
        //        return;
        //}
        //else if (cellNum < maxmount * 5 / 10)
        //{
        //    if (randomGrid.y > initialAgent.y + 2)//屋顶用
        //        return;
        //}
        //else if (cellNum < maxmount * 6 / 10)
        //{
        //    //overcrowdedNum1 = 10;
        //    if (randomGrid.y > initialAgent.y + 1)//屋顶用
        //        return;
        //}
        //else if (cellNum < maxmount * 7 / 10)
        //{
        //    if (randomGrid.y > initialAgent.y )//屋顶用
        //        return;
        //}
        //else if (cellNum < maxmount * 8 / 10)
        //{
        //    if (randomGrid.y > initialAgent.y - 1)//屋顶用
        //        return;
        //}
        //else if (cellNum < maxmount * 9 / 10)
        //{
        //    if (randomGrid.y > initialAgent.y-2)//屋顶用
        //        return;
        //}
        //else
        //{
        //    //overcrowdedNum1 = 12;
        //    if (randomGrid.y > initialAgent.y-3 )//屋顶用
        //        return;
        //}
        if (!NewCellNeighborConditions(randomGrid))
            return;

        SingleCell newCell = randomGrid.SpawnCell(subCellEnergy);//在对应的网格里生成新细胞
                                                                     
            cellNum++;
        
    }



    //DLA
    public void AddOneGridToReduceEnergy(SingleGrid cloestGrid,Vector3 growthDirection)
    {
        growthDirection.Normalize();

        Vector3 newPosition = cloestGrid.transform.position + growthDirection;
        SingleGrid gridToReduce = GetGridAroundCloestGrid(cloestGrid,newPosition);
        //SingleGrid gridToReduce = GetRandomGrid(cloestGrid);
        //if (gridToReduce = null)
        //    return;
        //if (allPrefab.IndexOf(gridToReduce) < 0)
        //{
        //    allPrefab.Add(gridToReduce);
        //}
        if (gridToReduce == null)
            return;
        allPrefab.Add(gridToReduce);
        //gridToReduce.SpawnCell(85);
        newPrefab++;
        //if(gridToReduce.gridEnergy==50)

        //gridToReduce.ReduceEnergy(50);
        //if(!gridToReduce.HasCell())

        //    gridToReduce.SpawnCell(100);
        //reduceGrid++;

    }
    public SingleGrid GetRandomGrid(SingleGrid cloestGrid)
    {
        List<SingleGrid> V5Grid = new List<SingleGrid>();
        for (int i = 0; i < 6; i++)
        {
            
            SingleGrid grid = GetGridAroundV6(cloestGrid, (GridsAroundV6)i);
            if (grid != null && grid.gridEnergy <= 50)
                V5Grid.Add(grid);
        }

        if (V5Grid.Count > 0)
        {
            int r = UnityEngine.Random.Range(0, V5Grid.Count);
            return V5Grid[r];
        }
        else
            return null;
        
            
        
        
    }
    public SingleGrid GetGridAroundCloestGrid(SingleGrid cloestGrid, Vector3 newPosition)
    {
        SingleGrid AroundCloestGrid = null;
        float minDis = 1000f;
        for (int i = 0; i < 6; i++)
        {
            SingleGrid aroundV5Grid = GetGridAroundV6(cloestGrid, (GridsAroundV6)i);
            if (aroundV5Grid != null && !aroundV5Grid.HasCell() && aroundV5Grid.gridEnergy <= 0)//判断条件控制生长数量
            {
                float dis = Vector3.Distance(newPosition, aroundV5Grid.transform.position);
                if (dis < minDis)
                {

                    AroundCloestGrid = aroundV5Grid;
                    minDis = dis;

                }

            }
            else
                continue;
          
        }
        
        return AroundCloestGrid;
        
    }

    public void AddDLAAgent()
    {
        Vector3 DLADirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * 200f;
        if (newPrefab < prefabNum)
        {

            Vector3 DLAtester = DLADirection;
            SingleGrid closestGrid = GetClosestGrid(DLAtester, allPrefab);
            Vector3 growthDirection = DLAtester - closestGrid.transform.position;
            AddOneGridToReduceEnergy(closestGrid, growthDirection);
            if (newPrefab >= prefabNum)
            {
                start = true;
                //return;
            }

        }
        

    }

    private SingleGrid GetClosestGrid(Vector3 DLAtester, List<SingleGrid> allPrefab)//calculate the closest cube from the dla tester
    {
        SingleGrid closestGeo = null;
        if (allPrefab.Count > 0)
        {
            float cloestDistance = 10000f;
            for (int i = 0; i < allPrefab.Count; i++)
            {
                SingleGrid currentGeo = allPrefab[i];
                float dist = Vector3.Distance(DLAtester, currentGeo.transform.position);
                if (dist < cloestDistance )
                {
                    closestGeo = currentGeo;
                    cloestDistance = dist;
                }
            }
        }
        return closestGeo;
    }

    public void DisPlayDLA()
    {
        foreach (SingleGrid grid in allPrefab)
        {
            grid.SpawnCell(60);
        }
    }
    public void ClearDLA()
    {
        foreach (SingleGrid grid in allPrefab)
        {
            if (grid.HasCell())
                grid.GetCell().Death();

        }
    }



    //Porosity change with environment condition
    public void CheckEnvironmentCondition()
    {
        if (overcrowdedNumUp > initialPorosity)
        {
            PorosityReduceScaffoldingVoidGrid();//温度降低 孔隙变小
            VoidGridAddGrid();
            initialPorosity = overcrowdedNumUp;
        }

        if (overcrowdedNumUp < initialPorosity)
        {
            PorosityAddScaffoldingVoidGrid();
            ReduceGrid();
            initialPorosity = overcrowdedNumUp;
        }
    }

    public void PorosityChangeScaffoldingDLA()
    {
        
        
        foreach (SingleGrid grid in allPrefab)
        {
            int neighbourNum = 0;
            for (int i = 0; i < 26; i++)
            {
                SingleGrid randomGrid = GetGridAroundV26(grid, (GridsAroundV26)i);

                if (randomGrid != null && randomGrid.HasCell())//dla grid 周围网格 是山地或者细胞
                     neighbourNum++;

            }
            if (neighbourNum < 16 && neighbourNum>12)
                PorosityDLA.Add(grid);
 
        }
    }

    public void PorosityReduceScaffoldingVoidGrid()//查找所有空网格
    {

        
        foreach (SingleGrid grid in grids)
        {
            if (grid.gridEnergy != 0|| grid.HasCell())
                continue;
            if (grid.x == initialAgent.x && grid.z == initialAgent.z)
                continue;
            if (allPrefab.IndexOf(grid) >= 0)//保留主管道
                continue;
            if (CountNeighbourNumRange(grid, 12, 16))
                PorosityVoidGrid.Add(grid);

        }
    }
    public void VoidGridAddGrid()
    {
        
        foreach (SingleGrid grid in PorosityVoidGrid)
        {
            grid.SpawnCell(100);
            newAddedCell++;
        }
    }

    public void PorosityAddScaffoldingVoidGrid()//查找所有空网格  找到最外围的网格 8-12 由外向内减 直到碰到dlagrid停止
    {


        foreach (SingleGrid grid in grids)
        {
            if (!grid.HasCell()||grid.GetCell().currentEnergy!=100)
                continue;
            //if (grid.x == initialAgent.x && grid.z == initialAgent.z)
            //    continue;
            //if (allPrefab.IndexOf(grid) >= 0)//保留主管道
            //    continue;
            if(CountNeighbourNumRange(grid,8,18))
                PorosityGrid.Add(grid);

        }
    }
    public bool CountNeighbourNumRange(SingleGrid grid,int a,int b)
    {
        int neighbourNum = 0;
        for (int i = 0; i < 26; i++)
        {
            SingleGrid randomGrid = GetGridAroundV26(grid, (GridsAroundV26)i);


            if (randomGrid != null && randomGrid.HasCell())//dla grid 周围网格 是山地或者细胞
                neighbourNum++;

        }
        if (neighbourNum < b && neighbourNum > a)
            return true;
        else
            return false;
    }

    public void ReduceGrid()
    {
        List<SingleGrid> arrayGrid = randomGetGridFromPorosityGrid(PorosityGrid, 20);
        
        foreach (SingleGrid grid in arrayGrid)
        {
            if (!grid.HasCell())
                continue;
            AddTunnel(grid,15);
            //grid.GetCell().Death();
            newReducedCell++;

        }
    }

    public List<SingleGrid> randomGetGridFromPorosityGrid(List<SingleGrid> PorosityGrid,int holeNum)//随机从porosityGrid里选出一定数量的grid
    {
        List<SingleGrid> array = new List<SingleGrid>();
        if (PorosityGrid.Count > 50)
        {
            for (int i = 0; i < holeNum;)
            {
                int index = UnityEngine.Random.Range(0, PorosityGrid.Count);
                SingleGrid grid = PorosityGrid[index];
                if(array.IndexOf(grid) >= 0)
                {
                    continue;
                }
                else
                {
                    array.Add(grid);
                    i++;
                }
                
            }
            return array;
        }
        else
        {
            array = PorosityGrid;
            return array;
        }
        
    }


    public void AddTunnel(SingleGrid grid,int step)//单个cell 走几步
    {
        grid.GetCell().Death();
        grid.SpawnCell(70);
        for (int i = 0; i < step; i++)
        {
            
            grid = RandomGetGridAroundGridV6(grid);
            
            if (grid == null)
                return;
            grid.GetCell().Death();
            //grid.SpawnCell(70);

            newReducedCell++;
            
        }
    }

    public SingleGrid RandomGetGridAroundGridV6(SingleGrid origon)//随机获取周围没有细胞的 能量值为0的网格
    {
        //随机获取有能量的网格

        List<SingleGrid> array = new List<SingleGrid>();
        for (int i = 0; i < 6; i++)
        {
            SingleGrid randomGrid = GetGridAroundV6(origon, (GridsAroundV6)i);
            //if (allPrefab.IndexOf(randomGrid) >= 0)//连接到DLA主管道
            //    return null;

            if (randomGrid != null && randomGrid.HasCell() && randomGrid.GetCell().currentEnergy == 100)//不是null 说明在网格cube里 而不是在外部环境 
                array.Add(randomGrid);

        }
        int number = array.Count;
        if (number != 0)
        {
            int random = UnityEngine.Random.Range(0, number);
            //return InverseGrid(array[random]);
            return array[random];
        }
        else
            return null;
    }


    public void StartSimulate()
    {
        StartCoroutine(SystemRun());
    }
    IEnumerator SystemRun()
    {
        
        while(newPrefab<prefabNum)
        {
            AddDLAAgent();
            
            yield return new WaitForSeconds(speed);//运行速度
            
        }
    }
    void Run()
    {
        
        if (currentRound ==0)
        {
            InitCells(initNum);
            //cellNum = initNum;
            currentRound++;

            //BottomGridReduceEnergy(grid,12);
            //initialReduceGrid = NumBottomGrid(grid,5);
            //allPrefab.Add(initialReduceGrid);

            //get bottom third grid bottom twelve grid added list and reduce energy
            BottomGridReduceEnergy(initialAgent, 3);
            SingleGrid bottom3Grid =NumBottomGrid(initialAgent, 3);
            BottomGridReducedList(bottom3Grid, 12);
            AboveGroundCellSpawned.Add(initialAgent);



            return;
        }
        //currentStep += 1;
        currentRound++;
        if (reduceGrid < maxmount)
        {
            foreach (SingleGrid grid in UndergroundGridReduced)
            {
                if (reduceGrid < maxmount)
                {
                    UndergroundNestGridReduce(grid);
                    
                }

                else
                    continue;
                
            }
            AddGridReducedToList();

        }



        if (cellNum < maxmount)
        {
            foreach (SingleGrid grid in AboveGroundCellSpawned)
            {
                
                if (cellNum < maxmount)
                {
                    
                    SingleCell cell = grid.GetCell();//寻找有细胞的网格 然后获取细胞 即初始生成的 initialcell
                    cell.AddCell();
                    //AddDLAAgent();
                    //CubicGridReduce(NumBottomGrid(grid, 12), 4);
                }
                else
                    continue;
            }
            AddCellSpawnedToList();
        }



        if (reduceGrid >= maxmount && cellNum >= maxmount)
        {
            
            SingleGrid bottom15Grid = NumBottomGrid(initialAgent, 15);
            CubicGridReduce(bottom15Grid, 1);
            simulationEnabled = false;
            return;
        }

        //if (cellNum >= maxmount)
        //{
        //    //CubicGridReduce(NumBottomGrid(initialReduceGrid, 10), 3);
        //    simulationEnabled = false;
        //    return;
        //}
    }
}

public enum GridsAroundV26
{
    LEFTUP, //0
    LEFT,  //1
    LEFTDOWN,//2
    UP,//3
    DOWN,//4
    RIGHTUP,//5
    RIGHT,//6
    RIGHTDOWN,//7

    FRONT,
    FRONTLEFTUP,
    FRONTLEFT, 
    FRONTLEFTDOWN,
    FRONTUP,
    FRONTDOWN,
    FRONTRIGHTUP,
    FRONTRIGHT,
    FRONTRIGHTDOWN,

    BEHIND,
    BEHINDLEFTUP, 
    BEHINDLEFT,  
    BEHINDLEFTDOWN,
    BEHINDUP,
    BEHINDDOWN,
    BEHINDRIGHTUP,
    BEHINDRIGHT,
    BEHINDRIGHTDOWN,
}

public enum GridsAroundV6
{
    
    LEFT,  //0   
    UP,
    DOWN,    
    RIGHT,
   
    BEHIND,
    FRONT,
}

public enum GridsAroundV5
{

    DOWN,
    LEFT,
    RIGHT,
    FRONT,
    BEHIND,
}

public enum GridsAroundV8
{
    
    LEFT,  //0
    
    
    
    RIGHT,//6
    

    FRONT,
    
    FRONTLEFT,
   
    FRONTRIGHT,
    

    BEHIND,
   
    BEHINDLEFT,
    
    BEHINDRIGHT,
    
}

public enum GridsAroundV14
{
    LEFTUP, //0
    LEFT,  //1
    LEFTDOWN,//2
    UP,//3
    DOWN,//4
    RIGHTUP,//5
    RIGHT,//6
    RIGHTDOWN,//7

    FRONT,
    FRONTUP,
    FRONTDOWN,
    BEHIND,
    BEHINDUP,
    BEHINDDOWN,



}

public enum GridsAroundV9
{
    
    LEFT,  //1
    LEFTDOWN,//2
    DOWN,//4
    RIGHT,//6
    RIGHTDOWN,//7
    FRONT,  
    FRONTDOWN,
    BEHIND, 
    BEHINDDOWN,

}