using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SingleCell : MonoBehaviour
{
    private SingleGrid grid;
    public MeshRenderer render;
    public float currentEnergy;
    public bool justBorn;
    private void Awake()
    {
        render.material = new Material(Shader.Find("Standard"));
    }
    public SingleGrid GetGrid()//获得细胞所在的网格 的singlegrid
    {
        return grid;
    }

    public void InitEnergy(float value)//细胞带有的能量
    {
        currentEnergy = value;
        UpdateColor();
    }
    public void SetGrid(SingleGrid g)//设置网格
    {
        grid = g;
    }
    public void AbsorbsEnergyFromEnv()////细胞从环境吸收能量 能量网格相对应减少能量
    {
        justBorn = false;
        float absorbNum;

        //if(currentEnergy < 4) //如果当前细胞能量小于4，从能量场吸收5点能量
            absorbNum = grid.gridEnergy;//吸收网格里的全部能量
        //else                  //如果当前细胞能量大于等于4，从能量场吸收10点能量
        //    absorbNum = 10f;
        //if (grid.gridEnergy < absorbNum)//如果网格能量小于细胞要吸收的能量，那么吸收网格所有剩余能量
        //    absorbNum = grid.gridEnergy;

        currentEnergy += absorbNum;
        grid.ReduceEnergy(absorbNum);//网格减少能量
        UpdateColor();
        
    }

    
    public void ReduceEnergy(float num)
    {
        if (currentEnergy - num >= 0)
            currentEnergy -= num;
        else
            currentEnergy = 0;
        if (IsDeathCondition())
            Death();
        UpdateColor();
    }
    public bool IsDeathCondition()
    {
        return currentEnergy < 2; //细胞小于1点能量就死亡
    }
    //optional 2.2
    public bool CanSplit()
    {
        return currentEnergy >100;//细胞大于1点能量就分裂
    }
    public bool CanMove()
    {
        return currentEnergy >= 100;//细胞大于1点能量就分裂
    }

    public void Death()
    {
        //Env.Instance.OnCellDeath(this);

        
        //grid.AddGridEnergy(currentEnergy * 9/10); //细胞死亡返还百分之九十的能量给能量场
        //grid.RemoveCell();//消除网格上对应的细胞
        Destroy(gameObject);
    }

    public void PickUpMaterialFromGround() //agent pick material
    {
        //justBorn = false;
        float absorbNum;

        SingleGrid gridGround = Env.Instance.BottomGrid(grid);
        absorbNum = gridGround.gridEnergy;//吸收网格里的全部能量


        currentEnergy += absorbNum;
        gridGround.ReduceEnergy(absorbNum);//网格材料消失 被清除
        UpdateColor();

    }

    

    public void DropMaterial()//agent drop material
    {
        //optional 2.2
        //if (!CanSplit())
        //    return;
        float subCellEnergy = 100;       
        currentEnergy -= subCellEnergy;//当前细胞能量要减去新生细胞能量
        
        UpdateColor();
        Env.Instance.Split(this, subCellEnergy);//克隆出分裂后的细胞 并带着母体的能量
        if (IsDeathCondition())
            Death();
    }

    

    public void AddCell()//agent 移动
    {
        //optional 2.2
        //if (!CanMove())
        //    return;
        float subCellEnergy = 100;
        //currentEnergy -= subCellEnergy;//当前细胞能量要减去新生细胞能量

        UpdateColor();
        Env.Instance.Split(this, subCellEnergy);//克隆出分裂后的细胞 并带着母体的能量
        if (IsDeathCondition())
            Death();
    }


    private void UpdateColor()//每个细胞颜色
    {
        if (currentEnergy <= 50)
            render.material.color = Env.Instance.cellColor1;
        else if(currentEnergy <= 80)
            render.material.color = Env.Instance.cellColor4;
        else if (currentEnergy <= 100)
            render.material.color = Env.Instance.cellColor2;
        else
            render.material.color = Env.Instance.cellColor3;


    }
}
