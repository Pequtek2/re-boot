using System.Collections.Generic;

// Definicja "Statyczna" - to co wczytujemy z JSONa
public class QuestDefinition
{
	public string Id;
	public string Title;
	public string Description; // Fabularny opis ("Kierownik kazał mi...")
	public List<QuestStage> Stages = new List<QuestStage>();
}

public class QuestStage
{
	public string Objective; // Co wyświetlić? np. "Znajdź bezpieczniki"
	public int RequiredAmount = 1; // Ile razy trzeba to zrobić? (Domyślnie 1)
}

// Stan "Dynamiczny" - to co zapisujemy w save gry
public class QuestState
{
	public string QuestId;
	public int CurrentStageIndex = 0; // Na którym etapie jesteśmy (0, 1, 2...)
	public int CurrentAmount = 0;     // Ile już zrobiliśmy w obecnym etapie (np. 2/5)
	public bool IsCompleted = false;
}
