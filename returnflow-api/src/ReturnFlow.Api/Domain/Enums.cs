namespace ReturnFlow.Api.Domain;

public enum Role { Customer, Agent, Manager, Admin }

public enum ReturnStatus { Submitted, Closed }

public enum ClaimStatus { Open, Approved, Rejected }

public enum DecisionOutcome { Approved, Rejected }

/// <summary>Where a claim is in the automated workflow.</summary>
public enum ProcessingState { Analyzing, AutoDecided, NeedsHuman, HumanReviewRequested, HumanDecided }

/// <summary>Who made a decision.</summary>
public enum Decider { Auto, Human }
