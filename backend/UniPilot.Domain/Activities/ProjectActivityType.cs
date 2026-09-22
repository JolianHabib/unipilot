namespace UniPilot.Domain.Activities;

public enum ProjectActivityType
{
    ProjectCreated,
    ProjectStatusChanged,
    DocumentUploaded,
    DocumentProcessed,
    DocumentProcessingFailed,
    RequirementsExtracted,
    RequirementCreated,
    RequirementCompleted,
    RequirementReopened,
    RequirementUpdated,
    RequirementDeleted,
    RequirementDuplicatesRemoved,
    TaskCreated,
    TaskUpdated,
    TaskMoved,
    TaskDeleted
}
