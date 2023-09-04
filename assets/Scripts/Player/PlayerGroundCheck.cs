using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundCheck : MonoBehaviour
{
    PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }
/*
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == playerController.gameObject) //check if the bottom trigger with its body
        {
            return;
        }
        playerController.SetGroundState(true);
    }
    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject == playerController.gameObject)
        {
            return;
        }
        playerController.SetGroundState(true);
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject == playerController.gameObject)
        {
            return;
        }
        playerController.SetGroundState(false);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == playerController.gameObject)
        {
            return;
        }
        playerController.SetGroundState(true);
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject == playerController.gameObject)
        {
            return;
        }
        playerController.SetGroundState(true);
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == playerController.gameObject)
        {
            return;
        }
        playerController.SetGroundState(false);
    }*/
}
