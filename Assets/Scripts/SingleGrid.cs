using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleGrid : MonoBehaviour
{
    private SingleCell cellInGrid;
    private GameObject cellCube;//right cell grid
    public MeshRenderer render;
    public float gridEnergy;
    public int x;
    public int y;
    public int z;
    
    private void Awake()
    {
        cellCube = Instantiate(Env.Instance.cellCubePrefab, transform.position+new Vector3(Env.Instance.spacing,0,0),Quaternion.identity,transform);//克隆细胞网格
        //cellCube = Instantiate(Env.Instance.cellCubePrefab,transform.position, Quaternion.identity, transform);//克隆细胞网格
        render.material = new Material(Shader.Find("Standard"));//渲染材质
        
        IfRender();
        UpdateColor();
    }

    private void Update()
    {
        IfRender();
    }
    public void AddGridEnergy(float value)//向网格注入能量 （从细胞处获得 或者 外界设置的初始能量）
    {
        gridEnergy += value;
        UpdateColor();
    }
    public void ReduceEnergy(float value)//网格失去能量
    {
        gridEnergy -= value;
        UpdateColor();
    }
    public void IfRender()
    {
        if (gridEnergy > 2)
            this.GetComponent<MeshRenderer>().enabled = true;
        else
            this.GetComponent<MeshRenderer>().enabled = false;
    }
    public void UpdateColor()  //更新能量场网格颜色
    {
        if (gridEnergy <2)
        {
            //Destroy(this);
            //render.material = new Material(RenderMode.)
            //GetComponent<MeshRenderer>().material.color = new Color(255, 255, 255, 0.5f);
            //SetMaterialRenderingMode(GetComponent<MeshRenderer>().material, RenderingMode.Transparent);

            //Color temp = Env.Instance.fullEnergyColor;
            //temp.g = 1 - gridEnergy / Env.Instance.initMotherEnergy;
            //temp.b = 1 - gridEnergy / Env.Instance.initMotherEnergy;
            //render.material.color = temp;
            render.material.color = new Color(100f/255f, 100f / 255f, 100f / 255f, 0.001f);

            SetMaterialRenderingMode(GetComponent<MeshRenderer>().material, RenderingMode.Transparent);
        }

        //else if (gridEnergy == 1)
        //{
        //    //Color temp = Env.Instance.fullEnergyColor;
        //    //temp.g = 1 - gridEnergy / Env.Instance.initMotherEnergy;
        //    //temp.b = 1 - gridEnergy / Env.Instance.initMotherEnergy;
        //    //render.material.color = temp;

        //    GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1, 1f);
        //    SetMaterialRenderingMode(GetComponent<MeshRenderer>().material, RenderingMode.Transparent);
        //}

        else 
        {
            //Color temp = Env.Instance.fullEnergyColor;
            //temp.g = 1 - gridEnergy / Env.Instance.initMotherEnergy;
            //temp.b = 1 - gridEnergy / Env.Instance.initMotherEnergy;
            //render.material.color = temp;
            //Color temp = Env.Instance.fullEnergyColor;
            //GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 1f);
            GetComponent<MeshRenderer>().material.color = Env.Instance.fullEnergyColor;
            SetMaterialRenderingMode(GetComponent<MeshRenderer>().material, RenderingMode.Opaque);
        }
        
    }
    public SingleCell SpawnCell(float energy)//生成新细胞
    {
        
        if (cellInGrid != null)
            return null;
        cellInGrid = Instantiate(Env.Instance.cellPrefab, cellCube.transform).GetComponent<SingleCell>();//在细胞网格的位置上 克隆细胞  将 gameobject 转换成 singlecell数据类型
        cellInGrid.SetGrid(this);//为每个 cellingrid 身上的脚本singlecell内的 grid 赋值   对应 cellingrid 当前的位置
        cellInGrid.InitEnergy(energy);//
        return cellInGrid;
    }

    public void RemoveCell()//消除细胞
    {
        cellInGrid = null;
        
    }

    public bool HasCell()//不是空的时候 说明有细胞
    {
        return cellInGrid != null;
    }
    public SingleCell GetCell()//获得每个 网格对应的细胞
    {
        if(cellInGrid!=null)
            return cellInGrid;
        return null;
    }
    public enum RenderingMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent,
    }

    public static void SetMaterialRenderingMode(Material material, RenderingMode renderingMode)
    {
        switch (renderingMode)
        {
            case RenderingMode.Opaque:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case RenderingMode.Cutout:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                break;
            case RenderingMode.Fade:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
            case RenderingMode.Transparent:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }
}
