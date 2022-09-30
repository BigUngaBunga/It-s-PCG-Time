using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class DiamondSquareAlgorithm
{
    private float[,] heightMap;
    private int size;
    private float roughness;
    private float Random => UnityEngine.Random.value;
    public DiamondSquareAlgorithm()
    {
        
    }

    public float[,] Generate(int size, int seed, float amplitude, float roughness)
    {
        this.size = size;
        this.roughness = roughness;
        heightMap = new float[size, size];
        UnityEngine.Random.InitState(seed);

        FillCorners(amplitude);
        FillSquare(amplitude, new Vector2(0, 0), new Vector2(size - 1, 0), new Vector2(0, size - 1), new Vector2(size - 1, size - 1));
        return heightMap;
    }

    private void FillCorners(float amplitude)
    {
        heightMap[0, 0] = Random * amplitude;
        heightMap[size - 1, 0] = Random * amplitude;
        heightMap[0, size - 1] = Random* amplitude;
        heightMap[size - 1, size - 1] = Random * amplitude;
    }

    //TODO gör den inte rekursiv
    //Alla värden måste räknas ut för ett "lager" innan nästa kan påbörjas
    //Ge avstånd till punkten som skall beräknas så kan den sköta det istället för att ge den koordinater.
    private void FillSquare(float amplitude, Vector2 upperLeftIndex, Vector2 upperRightIndex, Vector2 lowerLeftIndex, Vector2 lowerRightIndex)
    {
        Vector2 diamondIndex = GetDiamond(amplitude, upperLeftIndex, upperRightIndex, lowerLeftIndex, lowerRightIndex);
        Vector2 leftIndex = GetMiddle(amplitude, diamondIndex, upperLeftIndex, lowerLeftIndex);
        Vector2 upIndex = GetMiddle(amplitude, diamondIndex, upperLeftIndex, upperRightIndex);
        Vector2 rightIndex = GetMiddle(amplitude, diamondIndex, lowerRightIndex, upperRightIndex);
        Vector2 lowerIndex = GetMiddle(amplitude, diamondIndex, lowerRightIndex, lowerLeftIndex);

        //TODO kolla om det skall avbrytas
        FillSquare(amplitude, upperLeftIndex, upIndex, leftIndex, diamondIndex);//ÖV
        FillSquare(amplitude, upIndex, upperRightIndex, diamondIndex, rightIndex);//ÖH
        FillSquare(amplitude, lowerLeftIndex, rightIndex, upIndex, diamondIndex);//LV
        FillSquare(amplitude, lowerLeftIndex, rightIndex, upIndex, diamondIndex);//LH
    }

    private Vector2 GetDiamond(float amplitude, Vector2 upperLeftIndex, Vector2 upperRightIndex, Vector2 lowerLeftIndex, Vector2 lowerRightIndex)
    {
        Vector2 diamondIndex = new Vector2();



        return diamondIndex;
    }

    private Vector2 GetMiddle(float amplitude, Vector2 diamondIndex, Vector2 firstCornerIndex, Vector2 secondCornerIndex)
    {
        Vector2 middleIndex = new Vector2();

        
        return middleIndex;
    }

}