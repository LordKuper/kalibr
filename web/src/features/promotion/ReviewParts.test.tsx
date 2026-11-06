import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ConfirmedLevelText, type ReviewCompetency } from "./ReviewParts";

type ConfirmedLevel = NonNullable<ReviewCompetency["confirmedLevel"]>;

const CONFIRMED: ConfirmedLevel = {
  score: 3,
  scalePoints: [
    { score: 1, label: "Знаком с темой" },
    { score: 2, label: "Применяет с поддержкой" },
    { score: 3, label: "Применяет самостоятельно" },
    { score: 4, label: "Развивает других" },
  ],
  decidedAt: "2025-10-15T12:00:00Z",
  target: {
    unitId: "unit",
    unitName: "Управление разработки",
    matrixId: "matrix",
    trackId: "track",
    trackName: "Разработка",
    levelId: "level",
    levelName: "Разработчик",
  },
};

describe("confirmed level of a competency on the review card (AC-3)", () => {
  it("is one line with the score out of the top of its scale, the decision date and the level applied for", () => {
    render(<ConfirmedLevelText level={CONFIRMED} />);

    expect(
      screen.getByText("Подтверждено: 3 из 4, 15.10.2025 · Разработчик"),
    ).toBeInTheDocument();
  });

  it("draws nothing when the competency has no confirmation", () => {
    const { container } = render(<ConfirmedLevelText level={null} />);

    expect(container).toBeEmptyDOMElement();
  });
});
