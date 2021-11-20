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
    public SingleGrid GetGrid()//���ϸ�����ڵ����� ��singlegrid
    {
        return grid;
    }

    public void InitEnergy(float value)//ϸ�����е�����
    {
        currentEnergy = value;
        UpdateColor();
    }
    public void SetGrid(SingleGrid g)//��������
    {
        grid = g;
    }
    public void AbsorbsEnergyFromEnv()////ϸ���ӻ����������� �����������Ӧ��������
    {
        justBorn = false;
        float absorbNum;

        //if(currentEnergy < 4) //�����ǰϸ������С��4��������������5������
            absorbNum = grid.gridEnergy;//�����������ȫ������
        //else                  //�����ǰϸ���������ڵ���4��������������10������
        //    absorbNum = 10f;
        //if (grid.gridEnergy < absorbNum)//�����������С��ϸ��Ҫ���յ���������ô������������ʣ������
        //    absorbNum = grid.gridEnergy;

        currentEnergy += absorbNum;
        grid.ReduceEnergy(absorbNum);//�����������
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
        return currentEnergy < 2; //ϸ��С��1������������
    }
    //optional 2.2
    public bool CanSplit()
    {
        return currentEnergy >100;//ϸ������1�������ͷ���
    }
    public bool CanMove()
    {
        return currentEnergy >= 100;//ϸ������1�������ͷ���
    }

    public void Death()
    {
        //Env.Instance.OnCellDeath(this);

        
        //grid.AddGridEnergy(currentEnergy * 9/10); //ϸ�����������ٷ�֮��ʮ��������������
        //grid.RemoveCell();//���������϶�Ӧ��ϸ��
        Destroy(gameObject);
    }

    public void PickUpMaterialFromGround() //agent pick material
    {
        //justBorn = false;
        float absorbNum;

        SingleGrid gridGround = Env.Instance.BottomGrid(grid);
        absorbNum = gridGround.gridEnergy;//�����������ȫ������


        currentEnergy += absorbNum;
        gridGround.ReduceEnergy(absorbNum);//���������ʧ �����
        UpdateColor();

    }

    

    public void DropMaterial()//agent drop material
    {
        //optional 2.2
        //if (!CanSplit())
        //    return;
        float subCellEnergy = 100;       
        currentEnergy -= subCellEnergy;//��ǰϸ������Ҫ��ȥ����ϸ������
        
        UpdateColor();
        Env.Instance.Split(this, subCellEnergy);//��¡�����Ѻ��ϸ�� ������ĸ�������
        if (IsDeathCondition())
            Death();
    }

    

    public void AddCell()//agent �ƶ�
    {
        //optional 2.2
        //if (!CanMove())
        //    return;
        float subCellEnergy = 100;
        //currentEnergy -= subCellEnergy;//��ǰϸ������Ҫ��ȥ����ϸ������

        UpdateColor();
        Env.Instance.Split(this, subCellEnergy);//��¡�����Ѻ��ϸ�� ������ĸ�������
        if (IsDeathCondition())
            Death();
    }


    private void UpdateColor()//ÿ��ϸ����ɫ
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
