using Godot;
using System;
using System.Collections.Generic;
using System.Linq; // Ważne do obsługi list

public partial class QuestManager : Node
{
	public static QuestManager Instance { get; private set; }

	// Baza definicji (tylko do odczytu)
	private Dictionary<string, QuestDefinition> _definitions = new Dictionary<string, QuestDefinition>();
	
	// Aktualny stan gracza
	private Dictionary<string, QuestState> _activeQuests = new Dictionary<string, QuestState>();
	private HashSet<string> _completedQuests = new HashSet<string>();

	public event Action OnQuestsUpdated;
	public event Action<string, string> OnQuestNotification;

	public override void _Ready()
	{
		Instance = this;
		LoadQuestDefinitions();
	}

	private void LoadQuestDefinitions()
	{
		string text = FileAccess.GetFileAsString("res://Dialogue/quests.json");
		if (string.IsNullOrEmpty(text)) { GD.PrintErr("Brak pliku quests.json!"); return; }

		var json = Json.ParseString(text).AsGodotArray();

		foreach (var item in json)
		{
			var dict = item.AsGodotDictionary();
			var q = new QuestDefinition
			{
				Id = (string)dict["id"],
				Title = (string)dict["title"],
				Description = (string)dict["description"]
			};

			var stagesArr = (Godot.Collections.Array)dict["stages"];
			foreach (var stageRaw in stagesArr)
			{
				var stageDict = (Godot.Collections.Dictionary)stageRaw;
				q.Stages.Add(new QuestStage
				{
					Objective = (string)stageDict["objective"],
					RequiredAmount = stageDict.ContainsKey("amount") ? (int)stageDict["amount"] : 1
				});
			}
			_definitions.Add(q.Id, q);
		}
	}

	// --- LOGIKA ---

	public void StartQuest(string questId)
	{
		if (_definitions.ContainsKey(questId) && !_activeQuests.ContainsKey(questId) && !_completedQuests.Contains(questId))
		{
			var newState = new QuestState { QuestId = questId };
			_activeQuests.Add(questId, newState);
			
			NotifyUpdate("NOWE ZADANIE", _definitions[questId].Title);
		}
	}

	public void ProgressQuest(string questId, int amount = 1)
	{
		if (!_activeQuests.ContainsKey(questId)) return;

		var state = _activeQuests[questId];
		var def = _definitions[questId];
		var currentStage = def.Stages[state.CurrentStageIndex];

		// Zwiększ licznik
		state.CurrentAmount += amount;

		// Sprawdź czy etap zakończony
		if (state.CurrentAmount >= currentStage.RequiredAmount)
		{
			state.CurrentAmount = 0; // Reset licznika dla następnego etapu
			state.CurrentStageIndex++; // Następny etap

			// Czy to był ostatni etap?
			if (state.CurrentStageIndex >= def.Stages.Count)
			{
				CompleteQuest(questId);
			}
			else
			{
				NotifyUpdate("ZAKTUALIZOWANO", def.Title);
			}
		}
		else
		{
			// Tylko progres licznika (np. 1/3 -> 2/3)
			OnQuestsUpdated?.Invoke(); 
		}
	}

	private void CompleteQuest(string questId)
	{
		if (_activeQuests.ContainsKey(questId))
		{
			var title = _definitions[questId].Title;
			_activeQuests.Remove(questId);
			_completedQuests.Add(questId);
			
			NotifyUpdate("ZADANIE UKOŃCZONE", title);
		}
	}
	// Dodaj to do klasy QuestManager:
// Dodaj to do QuestManager.cs
public QuestState GetQuestState(string id)
{
	if (_activeQuests.ContainsKey(id)) return _activeQuests[id];
	// Jeśli zadanie jest w zakończonych, stwórz tymczasowy stan "completed" dla sprawdzenia
	if (_completedQuests.Contains(id)) return new QuestState { QuestId = id, IsCompleted = true };
	return null;
}
public void ForceCompleteQuest(string questId)
{
	CompleteQuest(questId);
}
	// Pomocnicze metody do UI
	private void NotifyUpdate(string type, string title)
	{
		OnQuestsUpdated?.Invoke();
		OnQuestNotification?.Invoke(type, title);
	}

	public QuestDefinition GetDefinition(string id) => _definitions.ContainsKey(id) ? _definitions[id] : null;
	public List<QuestState> GetActiveStates() => _activeQuests.Values.ToList();
}
