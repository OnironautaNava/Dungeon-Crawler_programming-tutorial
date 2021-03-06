﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;



public class GameManager : Singleton<GameManager> {

	// Revisar en qué nivel estamos
	// load and unload game levels
	// revisar en qué estado de juego estamos
	// generar otros persistent systems

	// PREGAME, RUNNING, PAUSED
	public enum GameState{
		PREGAME,
		RUNNING,
		PAUSED
	}

	public GameObject[] SystemPrefabs;
	public Events.EventGameState OnGameStateChanged;

	List<GameObject> _instancedSystemPrefabs;
	List<AsyncOperation> _loadOperations;
	GameState _currentGameState = GameState.PREGAME;
	private string _currentLevelName = string.Empty;

	public GameState CurrentGameState{
		get{ return _currentGameState; }
		private set{ _currentGameState = value;}
	}

	private void Start(){

		DontDestroyOnLoad(gameObject);//para asegurar que nuestro game manager permanecerá siempre

		_instancedSystemPrefabs = new List<GameObject>();
		_loadOperations = new List<AsyncOperation>();

		InstantiateSystemPrefabs();

		UIManager.Instance.OnMainMenuFadeComplete.AddListener(HandleMainMenuFadeComplete);
	}

	private void Update(){

		if(_currentGameState == GameState.PREGAME){
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Debug.Log("Se presionó Escape");
			TogglePause();
		}
	}

	void HandleMainMenuFadeComplete(bool fadeOut){
		if (!fadeOut)
		{
			UnloadLevel(_currentLevelName);	
		}
	}

	void UpdateState(GameState state){

		GameState previousGameState = _currentGameState;
		_currentGameState = state;

		switch(_currentGameState){
			case GameState.PREGAME:
				Time.timeScale = 1.0f;
			break;

			case GameState.RUNNING:
				Time.timeScale = 1.0f;
			break;

			case GameState.PAUSED:
				Time.timeScale = 0.0f;
			break;

			default:
			break;
		}

		OnGameStateChanged.Invoke(_currentGameState, previousGameState);
		// mandar mensajes
		// hacer transición de escenas
	}

	//Llenamos la lista de SystemPrefabs con todas las instancias
	void InstantiateSystemPrefabs(){

		GameObject prefabInstance;
		for (int i = 0; i < SystemPrefabs.Length; ++i)
		{
			prefabInstance = Instantiate(SystemPrefabs[i]);
			_instancedSystemPrefabs.Add(prefabInstance);
		}
	}

	void OnLoadOperationComplete(AsyncOperation ao){
		
		if (_loadOperations.Contains(ao))
		{
			_loadOperations.Remove(ao);
			if(_loadOperations.Count == 0){
				UpdateState(GameState.RUNNING);
			}
			
		}
		Debug.Log("Load complete.");
	}

	void OnUnLoadOperationComplete(AsyncOperation ao){
		
		Debug.Log("Unload complete.");
	}

	public void LoadLevel(string levelName){
		//Usamos la operación asyncrona para que haya ciertas operaciónes corriendo en segundo plano
		AsyncOperation ao = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
		if(ao == null){
			Debug.Log("[GameManager] Unable to load level " + levelName);
			return;
		}
		ao.completed += OnLoadOperationComplete;
		_loadOperations.Add(ao);
		_currentLevelName = levelName;
	}

	public void UnloadLevel(string levelName){

		AsyncOperation ao = SceneManager.UnloadSceneAsync(levelName);
		if(ao == null){
			Debug.Log("[GameManager] Unable to unload level " + levelName);
			return;
		}
		ao.completed += OnUnLoadOperationComplete;
	}

	protected override void OnDestroy(){

		base.OnDestroy();

		for (int i = 0; i < _instancedSystemPrefabs.Count; i++)
		{
			Destroy(_instancedSystemPrefabs[i]);
		}
		_instancedSystemPrefabs.Clear();
	}

	public void StartGame(){
		
		LoadLevel("Main");
	}

	public void TogglePause(){

		UpdateState( _currentGameState == GameState.RUNNING ? GameState.PAUSED : GameState.RUNNING);
	}

	public void RestartGame(){
		UpdateState(GameState.PREGAME); //Dejaremos que la máquina de estados haga el cambio de escenas
	}

	public void QuitGame(){
		//Aquí se pueden features al salir

		Application.Quit();
	}
}
