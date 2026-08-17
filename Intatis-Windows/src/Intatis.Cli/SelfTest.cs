using System.Text;
using System.Text.Json.Nodes;
using Intatis.Core;
using Intatis.Core.Cowork;
using Intatis.Core.Permission;
using Intatis.Core.Protocol;
using Intatis.Core.Providers;
using Intatis.Core.Session;
using Intatis.Core.Tools;

namespace Intatis.Cli;

/// <summary>Offline test suite: no network, no credentials, runs against temp dirs.</summary>
internal static class SelfTest
{
    private static int _passed;
    private static int _failed;

    public static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "intatis-selftest-" + IdGen.Random("run_"));
        Directory.CreateDirectory(root);
        try
        {
            TestJsoncStripping();
            TestConfigImport();
            TestPathConfinement(root);
            TestDeterministicGate();
            TestReviewerVerdictParsing();
            TestEventLog(root);
            TestProjectionFold(root);
            TestSessionProjection(root);
            TestSchedulerFifo();
            TestMediator();
            TestApplyPatch(root);
            TestChatLoop(root);
            TestWorkTaskGraph();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch (IOException) { }
        }

        Console.WriteLine($"\nselftest: {_passed} passed, {_failed} failed");
        return _failed == 0 ? 0 : 1;
    }

    private static void Check(string name, bool condition, string detail = "")
    {
        if (condition) { _passed++; Console.WriteLine($"  ok   {name}"); }
        else { _failed++; Console.WriteLine($"  FAIL {name} {detail}"); }
    }

    private static void TestJsoncStripping()
    {
        var jsonc = """
{
  // provider comment
  "model": "chat/gpt-4o-mini", /* block
  comment */
  "provider": {
    "chat": { "npm": "@ai-sdk/openai-compatible",
      "options": { "baseURL": "https://chat.example.com/v1", "apiKey": "{env:CHAT_API_KEY}", },
      "models": { "gpt-4o-mini": "Mini", },
    },
  },
}
""";
        var stripped = Jsonx.StripJsonc(jsonc);
        var obj = Jsonx.ParseObject(stripped);
        Check("jsonc: comments and trailing commas stripped",
            (string?)obj["model"] == "chat/gpt-4o-mini"
            && (string?)obj["provider"]?["chat"]?["options"]?["apiKey"] == "{env:CHAT_API_KEY}");
    }

    private static void TestConfigImport()
    {
        var config = """
{
  "enabled_providers": ["chat", "images"],
  "model": "chat/main-model",
  "permission_reviewer_model": "chat/reviewer-model",
  "image_model": "images/gpt-image-1",
  "provider": {
    "chat": {
      "npm": "@ai-sdk/openai-compatible",
      "options": { "baseURL": "https://chat.example.com/v1", "apiKey": "{env:CHAT_API_KEY}" },
      "models": {
        "main-model": { "name": "Main" },
        "reviewer-model": { "name": "Reviewer" }
      }
    },
    "images": {
      "npm": "@ai-sdk/openai-compatible",
      "options": { "baseURL": "https://images.example.com/v1", "apiKey": "{env:IMAGE_API_KEY}" },
      "models": { "gpt-image-1": { "name": "Images" } }
    },
    "disabled-provider": {
      "options": { "baseURL": "https://disabled.example.com/v1" },
      "models": {}
    }
  }
}
""";
        var environment = new Dictionary<string, string> { ["CHAT_API_KEY"] = "sk-test-value-123456" };
        var imported = ConfigImport.Parse(config, "test.json", environment);

        Check("config: providers parsed and filtered",
            imported.Providers.Count == 2
            && imported.Providers.All(p => p.Id is "chat" or "images"));
        Check("config: chat role resolved",
            imported.Chat is { ProviderId: "chat", ModelId: "main-model" });
        Check("config: reviewer binding resolved",
            imported.Reviewer is { ProviderId: "chat", ModelId: "reviewer-model" });
        Check("config: api key ref is env reference",
            imported.Provider("chat")?.ApiKeyRef.Source == SecretSource.Environment
            && imported.Provider("chat")?.ApiKeyRef.Value == "CHAT_API_KEY");
        Check("config: image model hidden from inference menu",
            imported.InferenceModels().Any(m => m.Id == "gpt-image-1") == false
            && imported.InferenceModels().Any(m => m.Id == "main-model"));

        var failClosed = ConfigImport.Parse("""{"model":"chat/main-model","permission_reviewer_model":"nope/missing","provider":{"chat":{"options":{"baseURL":"https://x.example/v1"},"models":{"main-model":"Main"}}}}""", "t.json", []);
        Check("config: unresolvable reviewer fails closed",
            failClosed.Reviewer is null && failClosed.ReviewerFailedClosed);

        var defaults = ConfigImport.Parse("""{"model":"ollama/llama3","provider":{"ollama":{"models":{"llama3":"Llama"}}}}""", "t.json", []);
        Check("config: built-in default base url for ollama",
            defaults.Provider("ollama")?.BaseUrl == "http://localhost:11434/v1");
    }

    private static void TestPathConfinement(string root)
    {
        var workspace = Path.Combine(root, "ws");
        Directory.CreateDirectory(workspace);
        File.WriteAllText(Path.Combine(workspace, "file.txt"), "hello");

        Check("confinement: inside accepted",
            PathConfinement.IsWithin(workspace, Path.Combine(workspace, "sub", "file.txt")));
        Check("confinement: escape rejected",
            PathConfinement.TryResolveWithin(workspace, "../outside.txt") is null);
        Check("confinement: sensitive path rejected",
            PathConfinement.TryResolveWithin(workspace, ".env") is null
            && PathConfinement.TryResolveWithin(workspace, "cert.pem") is null);
        Check("confinement: normal file accepted",
            PathConfinement.TryResolveWithin(workspace, "file.txt") is not null);
    }

    private static void TestDeterministicGate()
    {
        var root = "/w/workspace";
        var read = DeterministicPolicyGate.Evaluate(new ToolCallContext
        {
            ToolName = "read_file",
            SideEffect = SideEffect.ReadOnly,
            TouchedPaths = ["/w/workspace/a.txt"],
            RawArgs = "{}",
        }, root, PermissionProfile.Reviewed);
        Check("gate: read-only passes at low risk",
            read.Verdict == "pass" && read.Risk == RiskLevel.Low);

        var deny = DeterministicPolicyGate.Evaluate(new ToolCallContext
        {
            ToolName = "write_file",
            SideEffect = SideEffect.Write,
            TouchedPaths = ["/w/workspace/.env"],
            RawArgs = "{}",
        }, root, PermissionProfile.Reviewed);
        Check("gate: sensitive path denied", deny.Verdict == "deny");

        var locked = DeterministicPolicyGate.Evaluate(new ToolCallContext
        {
            ToolName = "write_file",
            SideEffect = SideEffect.Write,
            TouchedPaths = ["/w/workspace/a.txt"],
            RawArgs = "{}",
        }, root, PermissionProfile.Locked);
        Check("gate: locked profile denies writes", locked.Verdict == "deny");

        var escape = DeterministicPolicyGate.Evaluate(new ToolCallContext
        {
            ToolName = "write_file",
            SideEffect = SideEffect.Write,
            TouchedPaths = ["/etc/passwd"],
            RawArgs = "{}",
        }, root, PermissionProfile.Reviewed);
        Check("gate: escape denied", escape.Verdict == "deny");

        var readOnlyShell = DeterministicPolicyGate.Evaluate(new ToolCallContext
        {
            ToolName = "run_shell",
            SideEffect = SideEffect.Exec,
            RawArgs = """{"command":"git status --short"}""",
        }, root, PermissionProfile.Reviewed);
        Check("gate: read-only inspection command auto-allows",
            readOnlyShell.Verdict == "allow" && readOnlyShell.Risk == RiskLevel.Low);

        var dangerous = DeterministicPolicyGate.Evaluate(new ToolCallContext
        {
            ToolName = "run_shell",
            SideEffect = SideEffect.Exec,
            RawArgs = """{"command":"rm -rf /"}""",
        }, root, PermissionProfile.Reviewed);
        Check("gate: dangerous command denied", dangerous.Verdict == "deny");
    }

    private static void TestReviewerVerdictParsing()
    {
        var gate = GateResult.Pass(RiskLevel.Medium, "workspace write");
        var allow = ModelPermissionReviewer.ParseVerdict("writes only inside the workspace\nALLOW", gate);
        Check("reviewer: allow verdict parsed",
            allow.Decision == PermissionDecision.Allow && allow.Source == "automatic_reviewer");
        var deny = ModelPermissionReviewer.ParseVerdict("would overwrite lockfile\nDENY", gate);
        Check("reviewer: deny verdict parsed", deny.Decision == PermissionDecision.Deny);
        var protocol = ModelPermissionReviewer.ParseVerdict("```json\n{\"v\":1}\n```", gate);
        Check("reviewer: code fence violates protocol, falls back to user",
            protocol.Decision == PermissionDecision.AskUser);
        var noReason = ModelPermissionReviewer.ParseVerdict("ALLOW", gate);
        Check("reviewer: missing reason falls back to user",
            noReason.Decision == PermissionDecision.AskUser);
    }

    private static void TestEventLog(string root)
    {
        var file = Path.Combine(root, "sess_test1", "events.jsonl");
        using (var log = EventLog.Open("sess_test1", file))
        {
            var e1 = log.Append(EventType.UserMessage, new UserMessagePayload { Text = "hi" }.ToJson());
            var e2 = log.Append(EventType.MessageCompleted, new MessageCompletedPayload
            {
                MessageId = "msg_1", Text = "hello", Role = "assistant",
            }.ToJson());
            Check("eventlog: monotonic seq from zero",
                e1.Seq == 0 && e2.Seq == 1);

            var replay = log.Replay();
            Check("eventlog: replay matches appends",
                replay.Count == 2 && replay[0].Session == "sess_test1" && replay[1].Type == "message_completed");

            var unknown = log.Append("future_event_type", new JsonObject { ["x"] = 1 });
            Check("eventlog: unknown future type reserves seq",
                unknown.Seq == 2 && log.Replay().Count == 3);
        }

        using (var reopened = EventLog.Open("sess_test1", file))
        {
            Check("eventlog: reopen rescans tail",
                reopened.LastSeq == 2 && reopened.Replay().Count == 3);
        }

        using (var first = EventLog.Open("sess_test1", file))
        {
            var second = TryOpenAgain(file);
            Check("eventlog: writer lease is exclusive across runtimes", second is null);
        }
    }

    private static EventLog? TryOpenAgain(string file)
    {
        try { return EventLog.Open("sess_test1", file); }
        catch (EventLogException) { return null; }
    }

    private sealed class FakeChatProvider : IChatProvider
    {
        public List<string> Received { get; } = [];

        public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
            ChatRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var message in request.Messages)
                Received.Add($"{message.Role}:{message.Content}");
            await Task.Yield();
            yield return new ChatChunk.Delta("hel");
            yield return new ChatChunk.Delta("lo");
            yield return new ChatChunk.UsageReport(new Usage
            {
                PromptTokens = 10, CompletionTokens = 2, TotalTokens = 12,
            });
            yield return new ChatChunk.Done();
        }
    }

    private static void TestChatLoop(string root)
    {
        var file = Path.Combine(root, "sess_test2", "events.jsonl");
        using var log = EventLog.Open("sess_test2", file);
        var provider = new FakeChatProvider();
        var loop = new ChatLoop(log, provider, "test-model", systemPrompt: "be brief", includeUsage: true);
        loop.SendAsync("hi there").Wait();

        var projection = ConversationProjection.Build(log.Replay());
        Check("chatloop: user + assistant messages folded",
            projection.Count == 2
            && projection[0].Role == MessageRoleWire.User && projection[0].Text == "hi there"
            && projection[1].Text == "hello" && projection[1].IsComplete);
        Check("chatloop: history includes system + current turn",
            provider.Received.Count == 2
            && provider.Received[0].StartsWith("system:be brief")
            && provider.Received[^1] == "user:hi there");
        var outcome = log.Replay().Last(e => e.Type == EventType.TurnOutcome);
        Check("chatloop: turn outcome completed",
            (string?)outcome.Payload?["outcome"] == "completed");
    }

    private static void TestProjectionFold(string root)
    {
        var file = Path.Combine(root, "sess_test3", "events.jsonl");
        using var log = EventLog.Open("sess_test3", file);
        log.Append(EventType.UserMessage, new UserMessagePayload { Text = "q" }.ToJson());
        log.Append(EventType.MessageDelta, new MessageDeltaPayload { MessageId = "msg_a", TextDelta = "one " }.ToJson());
        log.Append(EventType.MessageDelta, new MessageDeltaPayload { MessageId = "msg_a", TextDelta = "two" }.ToJson());
        log.Append(EventType.Error, new ErrorPayload { Code = "provider", Message = "boom" }.ToJson());

        var projection = ConversationProjection.Build(log.Replay());
        Check("projection: open delta flushed as incomplete message",
            projection.Count == 3 && projection[1].Text == "one two" && projection[1].IsComplete == false);
        Check("projection: error rendered as system row",
            projection[2].Role == MessageRoleWire.System && projection[2].Text.Contains("boom"));
    }

    private static void TestSessionProjection(string root)
    {
        var file = Path.Combine(root, "sess_test4", "events.jsonl");
        using var log = EventLog.Open("sess_test4", file);
        SessionProjectionStore.UpdateDisplayName(log, SessionKind.Chat, "My Chat", changeKind: "created");
        SessionProjectionStore.UpdateDisplayName(log, SessionKind.Chat, "Renamed Chat");

        var document = SessionProjectionStore.Rebuild(log);
        Check("session.json: display name rebuilt from log",
            document.DisplayName == "Renamed Chat" && document.SettingsRevision == 2);
        Check("session.json: derived cache written",
            SessionProjectionStore.Load(file)?.DisplayName == "Renamed Chat");
    }

    private static void TestSchedulerFifo()
    {
        var scheduler = new AgentScheduler(maxConcurrentTasks: 2);
        var t1 = new ScheduledTask { Id = new TaskId("task_a"), Assignee = "alpha", Objective = "1", Input = "1" };
        var t2 = new ScheduledTask { Id = new TaskId("task_b"), Assignee = "beta", Objective = "2", Input = "2" };
        var t3 = new ScheduledTask { Id = new TaskId("task_c"), Assignee = "alpha", Objective = "3", Input = "3" };
        scheduler.Enqueue(t1);
        scheduler.Enqueue(t2);
        scheduler.Enqueue(t3);

        var first = scheduler.ClaimNext();
        var second = scheduler.ClaimNext();
        var third = scheduler.ClaimNext(); // alpha is busy with t1
        Check("scheduler: FIFO claim skips busy assignee",
            first?.Id.Value == "task_a" && second?.Id.Value == "task_b" && third is null);

        scheduler.RecordCompleted(first!, "done-1");
        var fourth = scheduler.ClaimNext();
        Check("scheduler: claim released on completion",
            fourth?.Id.Value == "task_c");

        var duplicate = scheduler.Enqueue(new ScheduledTask { Id = new TaskId("task_a"), Assignee = "alpha", Input = "x" });
        Check("scheduler: duplicate enqueue rejected", duplicate == false);
    }

    private static void TestMediator()
    {
        var mediator = new Mediator();
        Check("mediator: secrets blocked",
            mediator.Mediate("a", "b", "key sk-abcdefghijklmnop1234 here").Forwarded is null);
        Check("mediator: oversized content blocked",
            mediator.Mediate("a", "b", new string('x', 4001)).Forwarded is null);
        Check("mediator: normal content forwarded",
            mediator.Mediate("a", "b", "result: 42").Forwarded == "result: 42");
    }

    private static void TestWorkTaskGraph()
    {
        var graph = new WorkTaskGraph();
        var a = graph.Add(new WorkTask { Id = new WorkTaskId("wt_a"), Title = "A" });
        var b = graph.Add(new WorkTask
        {
            Id = new WorkTaskId("wt_b"), Title = "B",
            DependsOn = [new WorkTaskId("wt_a")],
        });
        Check("worktask: independent task ready",
            a.Status == WorkTaskStatus.Ready && b.Status == WorkTaskStatus.Pending);
        graph.Transition(a.Id, WorkTaskStatus.InProgress);
        graph.Transition(a.Id, WorkTaskStatus.Completed, result: "A done");
        Check("worktask: dependent becomes ready after completion",
            graph[new WorkTaskId("wt_b")].Status == WorkTaskStatus.Ready);

        var completedWithoutResult = false;
        try { graph.Transition(new WorkTaskId("wt_b"), WorkTaskStatus.Completed); }
        catch (InvalidOperationException) { completedWithoutResult = true; }
        Check("worktask: completion requires a result", completedWithoutResult);

        var cycle = false;
        try
        {
            graph.Add(new WorkTask
            {
                Id = new WorkTaskId("wt_c"), Title = "C",
                DependsOn = [new WorkTaskId("wt_d")],
            });
            graph.Add(new WorkTask
            {
                Id = new WorkTaskId("wt_d"), Title = "D",
                DependsOn = [new WorkTaskId("wt_c")],
            });
        }
        catch (InvalidOperationException) { cycle = true; }
        Check("worktask: dependency cycle rejected", cycle);
    }

    private static void TestApplyPatch(string root)
    {
        var workspace = Path.Combine(root, "patch-ws");
        Directory.CreateDirectory(workspace);
        File.WriteAllText(Path.Combine(workspace, "app.txt"), "alpha\nbeta\ngamma\n");

        var tool = new ApplyPatchTool();
        var context = new ToolContext { WorkspaceRoot = workspace };

        var add = tool.ExecuteAsync(JsonNode.Parse("""
{"patch": "*** Begin Patch\n*** Add File: new.txt\n+first\n+second\n*** End Patch"}
""")!, context).Result;
        Check("patch: add file",
            add.Text.Contains("applied") && File.ReadAllText(Path.Combine(workspace, "new.txt")) == "first\nsecond\n");

        var update = tool.ExecuteAsync(JsonNode.Parse("""
{"patch": "*** Begin Patch\n*** Update File: app.txt\n@@\n alpha\n-beta\n+delta\n gamma\n*** End Patch"}
""")!, context).Result;
        Check("patch: update in context",
            !update.Text.StartsWith("ERROR") && File.ReadAllText(Path.Combine(workspace, "app.txt")) == "alpha\ndelta\ngamma\n");

        var missing = tool.ExecuteAsync(JsonNode.Parse("""
{"patch": "*** Begin Patch\n*** Update File: app.txt\n@@\n nope\n-zzz\n*** End Patch"}
""")!, context).Result;
        Check("patch: context mismatch fails cleanly",
            missing.Text.StartsWith("ERROR"));

        var delete = tool.ExecuteAsync(JsonNode.Parse("""
{"patch": "*** Begin Patch\n*** Delete File: new.txt\n*** End Patch"}
""")!, context).Result;
        Check("patch: delete file",
            !delete.Text.StartsWith("ERROR") && !File.Exists(Path.Combine(workspace, "new.txt")));
    }
}
