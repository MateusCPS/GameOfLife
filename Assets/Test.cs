using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class Test : MonoBehaviour
{
    public ComputeShader shader, shaderPaint;

    public RenderTexture Result;
    public Texture2D texture;
    public Material material;

    public Color deadColor = Color.black, aliveColor = Color.white;

    public int x;

    public bool GPU, CPU;

    public bool isPaint;
    public bool isAlive;
    public float radius;
    void Start()
    {
        CreateTexture2D();
        Result = new RenderTexture(texture.width, texture.height, 0);
        Result.enableRandomWrite = true;
        Result.Create();

        shaderPaint.SetTexture(0, "Result", Result);
        shaderPaint.SetBool("isPaint", isPaint);
        shaderPaint.SetBool("isAlive", isAlive);
        shaderPaint.SetFloat("BrushPosX", Input.mousePosition.x);
        shaderPaint.SetFloat("BrushPosY", Input.mousePosition.y);
        shaderPaint.SetFloat("Radius", radius);
    }
    void Update()
    {
        PaintGPU();
        if (Input.GetMouseButton(0))
        {
            isPaint = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isPaint = false;
        }
        if (Input.GetMouseButtonDown(1))
        {
            isAlive = !isAlive;
        }

        if (GPU == true)
        {
            useGPU();
        }
        else if (CPU == true)
        {
            UseCPU();
        }
    }

    private void UseCPU()
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = texture.GetPixels();
        Color[] newPixels = new Color[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int index = x + y * width;
                int aliveNeighbors = CountAliveNeighbors(x, y, width, height, pixels);

                if (pixels[index] == aliveColor)
                {
                    if (aliveNeighbors == 2 || aliveNeighbors == 3)
                        newPixels[index] = aliveColor;
                    else
                        newPixels[index] = deadColor;
                }
                else
                {
                    if (aliveNeighbors == 3)
                        newPixels[index] = aliveColor;
                    else
                        newPixels[index] = deadColor;
                }
            }
        }

        Texture2D newTexture = new Texture2D(width, height);
        newTexture.SetPixels(newPixels);
        newTexture.Apply();

        texture = newTexture;
        material.mainTexture = texture;
        
    }

    private Texture2D CreateTexture2D()
    {
        texture = new Texture2D(x, x);
        texture.SetPixels(texture.GetPixels());
        texture.Apply();

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, deadColor);
            }
        }

        texture.Apply();
        material.mainTexture = texture;
        return texture;
    }

    private int CountAliveNeighbors(int x, int y, int width, int height, Color[] pixels)
    {
        int aliveNeighbors = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                // Ignorar a célula central (própria célula)
                if (i == 0 && j == 0)
                    continue;

                // Calcular as coordenadas do vizinho
                int neighborX = x + i;
                int neighborY = y + j;

                // Verificar se o vizinho está dentro dos limites da matriz
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    // Calcular o índice do vizinho na matriz unidimensional
                    int neighborIndex = neighborX + neighborY * width;

                    // Verificar se o vizinho está vivo (tem a cor do seu aliveColor)
                    if (pixels[neighborIndex] == aliveColor)
                    {
                        aliveNeighbors++;
                    }
                }
            }
        }

        return aliveNeighbors;
    }

    private void useGPU()
    {
        Graphics.Blit(texture, Result);

        int kernelIndex = 0;
        shader.SetTexture(kernelIndex, "Result", Result);
        shader.SetInt("Width", Result.width);
        shader.SetInt("Height", Result.height);

        shader.Dispatch(0, Result.width / 8, Result.height / 8, 1);

        RenderTexture.active = Result;
        texture.ReadPixels(new Rect(0, 0, Result.width, Result.height), 0, 0);
        texture.Apply();


        material.mainTexture = texture;

    }

    void PaintGPU()
    {

        shaderPaint.SetBool("isPaint", isPaint);
        shaderPaint.SetBool("isAlive", isAlive);
        shaderPaint.SetFloat("BrushPosX", Input.mousePosition.x);
        shaderPaint.SetFloat("BrushPosY", Input.mousePosition.y);
        shaderPaint.SetFloat("Radius", radius);
        shaderPaint.SetInt("_ScreenWidth", Result.width);
        shaderPaint.SetInt("_ScreenHeight", Result.height);
        shaderPaint.Dispatch(0, Result.width/8, Result.height/8, 1);

        RenderTexture.active = Result;

        texture.ReadPixels(new Rect(0, 0, Result.width, Result.height), 0, 0);
        texture.Apply();

        material.mainTexture = texture;

    }

}