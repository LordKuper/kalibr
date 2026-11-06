using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The level of a competency the chair last confirmed for the employee, as the review card shows it: taken from the latest decided application
/// of a session whose results were published, whatever its decision, never from the application under review, hidden while the session is not
/// published, read from that application's matrix version, and only told, never counted.
/// </summary>
public sealed class PromotionConfirmedLevelTests(PromotionHost fixture) : IClassFixture<PromotionHost>
{
    private const string Applications = "/api/v1/promotion/applications";
    private const string Concern = "confirmedLevel";
    private static readonly TimeSpan TimestampTolerance = TimeSpan.FromMilliseconds(1);

    [Fact]
    public async Task Ac1_Ac2_Ac5_Ac6_Ac7_TheCard_ConfirmsTheLatestPublishedDecisionOfAnotherApplication_OnlyForTheCommissionAndTheAdministrator_AndChangesNoScore()
    {
        var first = await fixture.NewSceneAsync();
        var raters = await first.NewRatersAsync(3, 2);
        var returning = await first.NewCandidateAsync();
        var newcomer = await first.NewCandidateAsync();
        var critical = first.Kit.Critical.Id;
        var profile = first.Kit.Profile.Id;
        await DecideSessionAsync(first, returning, raters, 4, 3, "Meets");
        var second = await first.AnnounceNextAsync();
        var secondDecision = await DecideSessionAsync(second, returning, raters, 2, 1, "DoesNotMeet");
        var third = await second.AnnounceNextAsync();
        var current = await third.SubmitForAdmissionAsync(returning, raters);
        var fresh = await third.SubmitForAdmissionAsync(newcomer, raters);
        await third.PutInStageAsync(3);
        await third.RateAsync(raters, current.Id, 4, 4);
        await third.RateAsync(raters, fresh.Id, 4, 4);
        await third.PutInStageAsync(5);
        var chair = third.Org.TeamHead.Client;

        var card = await ReviewAsync(chair, current.Id);
        var freshCard = await ReviewAsync(chair, fresh.Id);

        var confirmed = ConfirmedOf(card, critical)!;
        confirmed["score"]!.GetValue<int>().Should().Be(2, "the latest decision counts, and a decision that was not a pass is a decision too");
        ConfirmedOf(card, profile)!["score"]!.GetValue<int>().Should().Be(1);
        confirmed["decidedAt"]!.GetValue<DateTimeOffset>().Should().BeCloseTo(secondDecision.Json!["decidedAt"]!.GetValue<DateTimeOffset>(), TimestampTolerance);
        confirmed["target"]!["levelId"]!.GetValue<Guid>().Should().Be(first.Kit.Senior.Id);
        confirmed["scalePoints"]!.ToJsonString().Should().Be(CompetencyOf(card, critical)["scalePoints"]!.ToJsonString());
        foreach (var competency in freshCard.Json!["competencies"]!.AsArray())
        {
            competency![Concern].Should().BeNull("a candidate with no earlier decision has nothing confirmed");
        }

        Informational(card).Should().Be(Informational(freshCard), "a confirmation only informs: the rating, readiness and default scores do not move");

        var decided = await DecideAsync(third, current.Id, 3, 3, "Meets");
        ConfirmedOf(decided, critical)!["score"]!.GetValue<int>().Should().Be(2, "the application under review does not confirm its own scores");
        await third.PutInStageAsync(6);
        var published = await ReviewAsync(chair, current.Id);
        ConfirmedOf(published, critical)!["score"]!.GetValue<int>().Should().Be(2, "not even once its own results are published");

        await second.PutInStageAsync(5);
        var hidden = await ReviewAsync(chair, current.Id);
        ConfirmedOf(hidden, critical)!["score"]!.GetValue<int>().Should().Be(4, "while the session of the latest decision is unpublished, the one before it confirms");
        await second.PutInStageAsync(7);
        ConfirmedOf(await ReviewAsync(chair, current.Id), critical)!["score"]!.GetValue<int>().Should().Be(2, "publishing again brings the latest confirmation back");

        ConfirmedOf(await ReviewAsync(third.Org.Admin, current.Id), critical).Should().NotBeNull("the administrator reads the card too");
        foreach (var outsider in new[] { returning.Client, raters[0].Client, third.Org.Plain.Client })
        {
            (await outsider.GetAsync($"{Applications}/{current.Id}/review")).Status.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        }

        var candidateView = (await third.GetApplicationAsync(returning.Client, current.Id)).Expect(HttpStatusCode.OK);
        var ranking = (await chair.GetAsync($"/api/v1/promotion/waves/{third.WaveId}/units/{third.Org.Team.Id}/ranking")).Expect(HttpStatusCode.OK);
        candidateView.Json!.ToJsonString().Should().NotContain(Concern);
        ranking.Json!.ToJsonString().Should().NotContain(Concern);
    }

    [Fact]
    public async Task Ac4_TheConfirmation_FollowsACompetencyAcrossMatrixVersions_ByTheScaleItWasGiven_AndLeavesOutACompetencyTheNewVersionReplaced()
    {
        var first = await fixture.NewSceneAsync();
        var raters = await first.NewRatersAsync(3, 2);
        var candidate = await first.NewCandidateAsync();
        var kit = first.Kit;
        var admin = first.Org.Admin;
        var firstDecision = await DecideSessionAsync(first, candidate, raters, 4, 3, "Meets");

        var shortScale = await admin.MakeScaleAsync(points: CareerScenarios.Points(2));
        var critical = (await admin.GetAsync($"/api/v1/career/competencies/{kit.Critical.Id}")).Expect(HttpStatusCode.OK);
        (await admin.PutAsync(
            $"/api/v1/career/competencies/{kit.Critical.Id}",
            new
            {
                name = critical.Json!["name"]!.GetValue<string>(),
                description = (string?)null,
                directionId = critical.Json["directionId"]?.GetValue<Guid>(),
                scaleId = shortScale.Id,
            },
            critical.ETag)).Expect(HttpStatusCode.OK);
        var added = await admin.MakeCompetencyAsync(Scenarios.Unique("Новая"), null, Guid.Parse(CareerScenarios.BaseScaleId));
        var content = kit.Content(seniorCriticalScore: 2);
        Swap(content, "competencies", kit.Profile.Id, added.Id);
        Swap(content, "requirements", kit.Profile.Id, added.Id);
        await PublishVersionAsync(first, content);

        var second = await first.AnnounceNextAsync();
        var application = await SubmitSwappedAsync(second, candidate, raters, kit.Profile.Id, added.Id);
        await second.PutInStageAsync(3);
        foreach (var rater in raters)
        {
            (await rater.Client.PutAsync($"{Applications}/{application.Id}/rating", AnswersOf(2, kit.Critical.Id, added.Id))).Expect(HttpStatusCode.OK);
        }

        await second.PutInStageAsync(5);

        var card = await ReviewAsync(second.Org.TeamHead.Client, application.Id);
        var competencies = card.Json!["competencies"]!.AsArray();
        competencies.Select(competency => competency!["competencyId"]!.GetValue<Guid>()).Should().BeEquivalentTo([kit.Critical.Id, added.Id], "the competency the new version dropped is not shown");
        CompetencyOf(card, added.Id)[Concern].Should().BeNull("a competency nobody has confirmed stays without one");
        var shown = CompetencyOf(card, kit.Critical.Id);
        shown["scalePoints"]!.AsArray().Should().HaveCount(3, "the application is assessed by the new version, whose scale is the short one");
        var confirmed = shown[Concern]!;
        confirmed["score"]!.GetValue<int>().Should().Be(4);
        confirmed["decidedAt"]!.GetValue<DateTimeOffset>().Should().BeCloseTo(firstDecision.Json!["decidedAt"]!.GetValue<DateTimeOffset>(), TimestampTolerance);
        confirmed["scalePoints"]!.AsArray().Should().HaveCount(5, "the confirmation keeps the scale of the version it was given in");
    }

    [Fact]
    public async Task Ac4_TheConfirmation_OfACompetencyAbsentFromTheLatestApplication_ComesFromTheLatestOneThatHadIt()
    {
        var first = await fixture.NewSceneAsync();
        var raters = await first.NewRatersAsync(3, 2);
        var candidate = await first.NewCandidateAsync();
        var kit = first.Kit;
        var added = await first.Org.Admin.MakeCompetencyAsync(Scenarios.Unique("Новая"), null, Guid.Parse(CareerScenarios.BaseScaleId));
        await DecideSessionAsync(first, candidate, raters, 4, 3, "Meets");

        var swapped = kit.Content();
        Swap(swapped, "competencies", kit.Profile.Id, added.Id);
        Swap(swapped, "requirements", kit.Profile.Id, added.Id);
        await PublishVersionAsync(first, swapped);
        var second = await first.AnnounceNextAsync();
        var swappedApplication = await SubmitSwappedAsync(second, candidate, raters, kit.Profile.Id, added.Id);
        await second.PutInStageAsync(3);
        foreach (var rater in raters)
        {
            (await rater.Client.PutAsync($"{Applications}/{swappedApplication.Id}/rating", AnswersOf(4, kit.Critical.Id, added.Id))).Expect(HttpStatusCode.OK);
        }

        await second.PutInStageAsync(5);
        var chair = second.Org.TeamHead.Client;
        var review = await ReviewAsync(chair, swappedApplication.Id);
        var finalScores = new[] { kit.Critical.Id, added.Id }.Select(competencyId => new { competencyId, score = 2 }).ToArray();
        (await chair.PutAsync($"{Applications}/{swappedApplication.Id}/review", new { finalScores, decision = "DoesNotMeet" }, review.ETag)).Expect(HttpStatusCode.OK);
        await second.PutInStageAsync(7);

        await PublishVersionAsync(second, kit.Content());
        var third = await second.AnnounceNextAsync();
        var current = await third.SubmitForAdmissionAsync(candidate, raters);
        await third.PutInStageAsync(3);
        await third.RateAsync(raters, current.Id, 4, 4);
        await third.PutInStageAsync(5);

        var card = await ReviewAsync(chair, current.Id);

        ConfirmedOf(card, kit.Profile.Id)!["score"]!.GetValue<int>().Should().Be(3, "the latest application lacked the competency, so the first one that had it confirms");
        ConfirmedOf(card, kit.Critical.Id)!["score"]!.GetValue<int>().Should().Be(2, "a competency the latest application had is confirmed by it");
    }

    private static async Task PublishVersionAsync(PromotionScene scene, JsonNode content)
    {
        var kit = scene.Kit;
        var version = (await kit.Author.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created).Json!["id"]!.GetValue<Guid>();
        (await kit.SaveAsync(kit.Author, content, versionId: version)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(scene.Org.TeamHead.Client, "submit", versionId: version)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(scene.Org.DepartmentHead.Client, "approve", versionId: version)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(scene.Org.Hr.Client, "approve", versionId: version)).Expect(HttpStatusCode.OK);
    }

    private static async Task<ApiResponse> ReviewAsync(ApiClient who, Guid applicationId) =>
        (await who.GetAsync($"{Applications}/{applicationId}/review")).Expect(HttpStatusCode.OK);

    private static JsonNode CompetencyOf(ApiResponse review, Guid competencyId) =>
        review.Json!["competencies"]!.AsArray().Single(competency => competency!["competencyId"]!.GetValue<Guid>() == competencyId)!;

    private static JsonNode? ConfirmedOf(ApiResponse review, Guid competencyId) => CompetencyOf(review, competencyId)[Concern];

    private static string Informational(ApiResponse review) =>
        string.Join(
            '|',
            review.Json!["rating"],
            review.Json["isReady"],
            review.Json["isRatingFromFinalScores"],
            string.Join(',', review.Json["competencies"]!.AsArray().Select(competency => $"{competency!["defaultFinalScore"]}/{competency["finalScore"]}/{competency["isMet"]}")));

    private static async Task<ApiResponse> DecideAsync(PromotionScene scene, Guid applicationId, int critical, int additional, string decision)
    {
        var chair = scene.Org.TeamHead.Client;
        var review = await ReviewAsync(chair, applicationId);
        return (await chair.PutAsync($"{Applications}/{applicationId}/review", scene.ReviewBody(critical, additional, decision), review.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// Takes one candidate through a whole session of the scene to a decision of the chair and leaves the session over, its results published.
    /// </summary>
    private static async Task<ApiResponse> DecideSessionAsync(PromotionScene scene, Member candidate, Member[] raters, int critical, int additional, string decision)
    {
        var application = await scene.SubmitForAdmissionAsync(candidate, raters);
        await scene.PutInStageAsync(3);
        await scene.RateAsync(raters, application.Id, 4, 4);
        await scene.PutInStageAsync(5);
        var decided = await DecideAsync(scene, application.Id, critical, additional, decision);
        await scene.PutInStageAsync(7);
        return decided;
    }

    /// <summary>
    /// Files, submits and admits an application for a matrix version in which one competency of the scene's kit stands in place of another.
    /// </summary>
    private static async Task<ApiResponse> SubmitSwappedAsync(PromotionScene scene, Member candidate, Member[] raters, Guid replaced, Guid replacement)
    {
        var body = JsonSerializer.SerializeToNode(scene.ApplicationBody(raters, criticalScore: 2))!;
        Swap(body, "selfScores", replaced, replacement);
        var draft = (await candidate.Client.PostAsync(Applications, body)).Expect(HttpStatusCode.Created);
        var submitted = (await candidate.Client.PostAsync($"{Applications}/{draft.Id}/submit", ifMatch: draft.ETag)).Expect(HttpStatusCode.OK);
        return (await scene.Org.Hr.Client.PutAsync(
            $"{Applications}/{submitted.Id}/formal",
            new { hiredOrPromotedOn = new DateOnly(2020, 1, 1), ignoreInterval = false },
            submitted.ETag)).Expect(HttpStatusCode.OK);
    }

    private static object AnswersOf(int score, params Guid[] competencyIds) =>
        new { answers = competencyIds.Select(competencyId => new { competencyId, score }).ToArray() };

    private static void Swap(JsonNode body, string list, Guid competencyId, Guid replacement)
    {
        foreach (var item in body[list]!.AsArray().Where(item => item!["competencyId"]!.GetValue<Guid>() == competencyId))
        {
            item!["competencyId"] = replacement;
        }
    }
}
