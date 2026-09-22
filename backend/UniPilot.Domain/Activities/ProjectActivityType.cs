namespace UniPilot.Domain.Activities;

public enum ProjectActivityType
{
    ProjectCreated,
    DocumentUploaded,
    DocumentProcessed,
    DocumentProcessingFailed,
    RequirementsExtracted,
    RequirementCompleted,
    RequirementReopened,
    RequirementUpdated,
    RequirementDeleted,
    TaskCreated,
    TaskUpdated,
    TaskMoved,
    TaskDeleted
}