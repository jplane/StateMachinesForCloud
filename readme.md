# State Machines For Cloud

State Machines For Cloud (SM4C) is a cloud-native state machine implementation inspired by existing specifications like [SCXML](https://www.w3.org/TR/scxml/) and [CNCF Serverless Workflows](https://serverlessworkflow.io/), and projects like [XState](https://xstate.js.org/).

SM4C defines states, transitions, actions, data, and event-handling as first-class concepts. There are no explicit constructs for error-handling or compensation; instead, its intended that state machine authors account for any such logic explicitly in their design. Hosting and invocation of state machine instances is also left as an exercise for integrators; SSM can be hosted in any .NET Core process.

## Getting Started

*helloworld.json*
```JSON
{
  "id": "helloworld",
  "version": "1.0",
  "name": "Hello World Workflow",
  "description": "Inject Hello World",
  "states": [
    {
      "name": "Hello State",
      "start": true,
      "enterAction": {
        "type": "injectData",
        "expression": "${ \"Hello World!\" }"
      },
      "enterResultHandler": "${ .result += $value }"
    }
  ]
}
```

*main.cs*
```csharp

var host = GetStateMachineHost();   // implementation of [IStateMachineHost](/Integration/IStateMachineHost.cs)

var definition = File.ReadAllText("helloworld.json");

var sm = JsonConvert.DeserializeObject<StateMachine>(definition);

var json = await StateMachineRunner.RunAsync(sm, host);

```

## Core Concepts
---
### StateMachine

StateMachine is the top-level entity in the model. It contains collections of states, event and function defintions, and retry policies.

[code](/Model/StateMachine.cs)

#### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | state machine name | yes |
| description | string | state machine description | no |
| version | string | state machine version | yes |
| timeout | ExecutionTimeoutPolicy | timeout and action rules for state machine execution | no |
| events | string (URI) OR array of EventDefinition | incoming and outgoing event definitions for this state machine | no |
| functions | string (URI) OR array of FunctionDefinition | function definitions for this state machine | no |
| retries | string (URI) OR array of RetryPolicy | retry policies for this state machine | no |
| states | array of State | state definitions for this state machine | yes |

#### Example

```JSON
{
  "id": "helloworld",
  "version": "1.0",
  "name": "Hello World Workflow",
  "description": "Inject Hello World",
  "states": [
    {
      "name": "Hello State",
      "start": true,
      "enterAction": {
        "type": "injectData",
        "expression": "${ \"Hello World!\" }"
      },
      "enterResultHandler": "${ .result += $value }"
    }
  ]
}
```

---
### State

States are the specific "situations" in which a system can exist during its lifetime. States can define behaviors (actions) that occur upon entrance to or exit from the state, as well as the set of legal transitions from that state to other states.

[code](/Model/State.cs)

#### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | state name | yes |
| start | boolean | is this a start state? | no |
| transitions | array of Transition | transitions from this state to others | no |
| inputFilter | JQ expression (string) | filter global data for child actions | no |
| enterAction | Action | action to invoke upon entering this state | no |
| enterResultHandler | JQ expression (string) | merge enter action result with global data | no |
| exitAction | Action | action to invoke upon exiting this state | no |
| exitResultHandler | JQ expression (string) | merge exit action result with global data | no |

#### Example

```JSON
...
{
    "name": "UndoFlightReservation",
    "enterAction": {
        "type": "InvokeFunction",
        "functionName": "callFlightReservationUndoMicroservice",
        "arguments": {
            "flight": "${ .bookedFlight }"
        }
    },
    "transitions": [
        {
            "nextState": "ReportError"
        }
    ]
}
...
```
---
### Transition

Transitions define control flow between states in the system. A transition is always defined in context of the source transition (that is, the state *from* which control flows).

Transitions may be implicit, where the system moves from one state to another automatically. They may have a logical condition that guards the transition (the condition is evaluated against global state machine data). They may have one or more events whose arrival triggers the transition. Or, they may define a timeout whose expiration triggers the transition.

[code](/Model/Transition.cs)

#### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| nextState | string | name of state to transition *to* | no |
| condition | JQ expression (string) | evaluated against global data; result of 'true' triggers this transition | no |
| eventGroups | array of EventGroup | set of events that trigger this transition | no |
| timeout | timespan | elapsed time after which this transition is triggered | no |
| action | Action | action invoked when transition is triggered | no |
| resultHandler | JQ expression (string) | merge action result with global data | no |

### Example

```JSON
...
{
    "nextState": "EvaluateDecision",
    "eventGroups": [
        {
            "events": [ "CreditCheckCompletedEvent" ],
            "resultHandler": "${ .creditCheck += ($value | .Data) }"
        }
    ]
}
...
```
---
### Action

Actions represent the behavior or side effects that occur during state machine execution, as transitions between states are triggered. Actions can be associated with entry to and exit from states, as well as upon transition triggering.

All actions produce a well-defined JSON output, which can be optionally merged into global state machine data. Some actions are hierarchical, in that they can define control flow, but only between child actions *within the parent state*.

[code](/Model/Actions/Action.cs)

#### Action types

| Action | Description | Hierarchical? | JSON output |
| --- | --- | --- | --- |
| [delay](#DelayAction) | pause state machine execution for a defined timeout | no | null |
| [foreach](#ForEachAction) | execute child action for each element of an array | yes | an array with output for each input array element |
| [injectData](#InjectDataAction) | create arbitrary JSON | no | object/array/value/null |
| [invokeFunction](#InvokeFunctionAction) | invoke a JQ, OpenAPI, or gRPC function | no | object/array/value/null |
| [invokeSubflow](#InvokeSubflowAction) | invoke another state machine | no | object/array/value/null |
| [parallel](#ParallelAction) | execute child actions at the same time | yes | object with property names mapped to child action name/index |
| [sendEvent](#SendEventAction) | publish an event to an external system | no | null |
| [sequence](#SequenceAction) | execute child actions one after another | yes | object with property names mapped to child action name/index |
---
#### DelayAction

[code](/Model/Actions/DelayAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "delay" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| timeout | timespan | state machine pause duration | yes |

##### Example

```JSON
...
{
    "timeout": "00:00:10"
}
...
```
---
#### ForEachAction

[code](/Model/Actions/ForEachAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "foreach" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| input | JQ expression (string) | selects an array element from global state machine data | yes |
| action | Action | action invoked for each element of input array | yes |
| maxParallel | integer | max degree of parallelism (default = 1) | no |

##### Example

```JSON
...
{
    "input": "${ .myObject.myArray }",
    "type": "foreach",
    "action": {
        "type": "injectData",
        "expression": "${ \"Hello World!\" }"
    }
}
...
```
---
#### InjectDataAction

[code](/Model/Actions/InjectDataAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "injectData" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| expression | JQ expression (string) | selects/creates arbitrary JSON using global state machine data | yes |

##### Example

```JSON
{
    "type": "injectData",
    "expression": "${ \"Hello World!\" }"
}
```
---
#### InvokeFunctionAction

[code](/Model/Actions/InvokeFunctionAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "invokeFunction" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| functionName | string | reference to an existing function definition | yes |
| timeout | timespan | max wait duration, ignored if 'waitForCompletion' is false. if timeout is reached, result is null | no |
| waitForCompletion | boolean | if true, state machine waits for function to complete and return a value or error. if false, state machine assumes null result and immediately proceeds (default = false) | no |
| arguments | map of (string, object) | inputs for function evaluation; values can be JQ expressions, which will be evaluated against global state machine data | no |

##### Example

```JSON
...
{
    "type": "invokeFunction",
    "functionName": "provisionOrderFunction",
    "arguments": {
        "order": "${ .orders[1] }"
    }
}
...
```
---
#### InvokeSubflowAction

[code](/Model/Actions/InvokeSubFlowAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "invokeSubflow" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| subflowName | string | name of an externally defined state machine to execute | yes |
| timeout | timespan | max wait duration, ignored if 'waitForCompletion' is false. if timeout is reached, result is null | no |
| waitForCompletion | boolean | if true, state machine waits for subflow to complete and return a value or error. if false, state machine assumes null result and immediately proceeds (default = false) | no |
| arguments | map of (string, object) | inputs for subflow execution; values can be JQ expressions, which will be evaluated against global state machine data | no |

##### Example

```JSON
...
{
    "type": "invokeSubflow",
    "subflowName": "myOtherStateMachine",
    "arguments": {
        "order": "${ .someObject }"
    }
}
...
```
---
#### ParallelAction

[code](/Model/Actions/ParallelAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "parallel" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| actions | array of Action | actions to invoke in parallel | yes |
| completionType | 'and' or 'xor' or 'n_of_m' | 'and' = wait for completion of all actions, 'xor' = wait for completion of one branch, 'n_of_m' = wait for completion of N branches (default = 'and') | no |
| n | integer | 'n' value when completionType = 'n_of_m' | no |

##### Example

```JSON
...
{
    "type": "parallel",
    "completionType": "and",
    "actions": [
        {
            "type": "invokeFunction",
            "functionName": "callHotelReservationMicroservice",
            "arguments": {
                "hotel": "${ .hotelDetails }",
                "flight": "${ .bookedFlight }"
            }
        },
        {
            "type": "invokeFunction",
            "functionName": "callAutoReservationMicroservice",
            "arguments": {
                "auto": "${ .autoDetails }",
                "flight": "${ .bookedFlight }"
            }
        }
    ]
}
...
```
---
#### SendEventAction

[code](/Model/Actions/SendEventAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "sendEvent" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| event | string | name of event definition in the state machine | yes |
| expression | JQ expression (string) | selects/creates JSON element using global state machine data, defines event body | no |
| contextAttributes | map of (string,string) | defines correlation key/value pairs for outgoing CloudEvent | no |

##### Example

```JSON
...
{
    "type": "sendEvent",
    "event": "orderCompletedEvent",
    "expression": "${ .order }"
}
...
```
---
#### SequenceAction

[code](/Model/Actions/SequenceAction.cs)

##### Definition

| Attribute | Datatype | Description | Required? |
| --- | --- | --- | --- |
| name | string | name of action | no |
| type | string | "sequence" | yes |
| errorHandlers | array of ErrorHandler | error handlers defined for this action | no |
| actions | array of Action | actions to invoke one after another | yes |

##### Example

```JSON
...
{
    "type": "sequence",
    "actions": [
        {
            "type": "invokeFunction",
            "functionName": "callHotelReservationMicroservice",
            "arguments": {
                "hotel": "${ .hotelDetails }",
                "flight": "${ .bookedFlight }"
            }
        },
        {
            "type": "invokeFunction",
            "functionName": "callAutoReservationMicroservice",
            "arguments": {
                "auto": "${ .autoDetails }",
                "flight": "${ .bookedFlight }"
            }
        }
    ]
}
...
```
---
### Data

All data in SM4C is manipulated as JSON, using [JQ](https://stedolan.github.io/jq/) expressions for reads and writes.

A state machine instance accepts an (optional) JSON input and produces a JSON output upon completion.

Data references within actions (function arguments, etc.) are defined using static values or JQ expressions evaluated against the initial ("global") state machine input. Action outputs can also be merged back into global state machine data using JQ expressions.

[States](#State) can further define an (optional) inputFilter attribute to narrow the data available to child actions; when this happens, JQ expressions to resolve action arguments, etc. evaluate against this narrowed data.

---
### Execution Algorithm

1. if the state machine has exactly one state with start = true, that is the start state, else error.
1. if state.InputFilter is not null, apply the filter to global state machine data and use the result as "global data" for remainder of this state's execution
1. if state.EnterAction is not null, invoke the action using global data as input
1. is state.EnterResultHandler is not null, apply the handler to merge enter action result into state machine data
1. if the state has zero transitions, the state machine ends
1. if the state has exactly one implicit transition, it is triggered (goto 9):
    - no condition
    - no eventGroups
    - no timeout
1. for each transition with a condition but no defined eventGroups or timeouts, the first such transition where condition evaluates to true is triggered (goto 9)
1. for each transition with no condition or timeout, but with defined eventGroups, the first such transition with matched events is triggered (goto 9)
    - within an event group, the arrival of *any* defined event satisfies the event group match
    - a transition is triggered when *all* its event groups are matched
    - while waiting for event arrival, if a single transition exists with defined timeout but no defined condition or eventGroups, that transition is triggered if its timeout duration is reached
1. if triggeredTransition.Action is not null, invoke the action using global data as input
1. if triggeredTransition.ResultHandler is not null, apply the handler to merge action result into state machine data
1. if triggeredTransition.NextState is null, goto 5
1. if state.ExitAction is not null, invoke the action using global data as input
1. is state.ExitResultHandler is not null, apply the handler to merge enter action result into state machine data
1. resolve triggeredTransition.NextState and goto 2
---
