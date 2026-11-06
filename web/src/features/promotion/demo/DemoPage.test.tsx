import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Providers } from "../../../app/Providers";
import { LONG_FLOW_TIMEOUT } from "../../../test/timeouts";
import { DemoPage } from "./DemoPage";
import { DEMO_STEPS } from "./scenario";

describe("demo of the career sessions (AC-44)", () => {
  it("plays every step of the scenario without sending a single request", {
    timeout: LONG_FLOW_TIMEOUT * 3,
  }, async () => {
    const network = vi.spyOn(globalThis, "fetch");
    try {
      render(
        <Providers>
          <MemoryRouter>
            <DemoPage />
          </MemoryRouter>
        </Providers>,
      );
      const user = userEvent.setup();

      for (const [index, step] of DEMO_STEPS.entries()) {
        expect(
          screen.getByText(`Шаг ${index + 1} из ${DEMO_STEPS.length}`, {
            exact: false,
          }),
        ).toBeInTheDocument();
        expect(
          screen.getAllByRole("heading", { level: 2, name: step.title }),
        ).not.toHaveLength(0);
        if (index < DEMO_STEPS.length - 1) {
          await user.click(screen.getByRole("button", { name: "Далее" }));
        }
      }

      expect(screen.getByRole("button", { name: "Далее" })).toBeDisabled();
      expect(network).not.toHaveBeenCalled();
    } finally {
      network.mockRestore();
    }
  });
});

describe("confirmed level in the demo of the career sessions (AC-3, AC-8)", () => {
  it.each(["Карточка кандидата", "Итоговые оценки и решение"])(
    "shows the confirmed level of one competency only on the step «%s»",
    { timeout: LONG_FLOW_TIMEOUT * 3 },
    async (title) => {
      render(
        <Providers>
          <MemoryRouter>
            <DemoPage />
          </MemoryRouter>
        </Providers>,
      );
      const user = userEvent.setup();
      const target = DEMO_STEPS.findIndex((step) => step.title === title);
      expect(target).toBeGreaterThan(0);

      for (let index = 0; index < target; index++) {
        await user.click(screen.getByRole("button", { name: "Далее" }));
      }

      const lines = screen.getAllByText(/^Подтверждено:/);
      expect(lines).toHaveLength(1);
      expect(lines[0]).toHaveTextContent(
        /^Подтверждено: 3 из 4, .+ · Разработчик$/,
      );
    },
  );
});
