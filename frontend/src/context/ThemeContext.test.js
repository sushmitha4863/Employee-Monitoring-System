import { act, fireEvent, render, screen } from "@testing-library/react";
import { ThemeProvider, useTheme } from "./ThemeContext";

function ThemeProbe() {
  const { theme, setTheme, toggleTheme } = useTheme();
  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <button onClick={() => setTheme("light")}>Light</button>
      <button onClick={() => setTheme("dark")}>Dark</button>
      <button onClick={toggleTheme}>Toggle</button>
    </div>
  );
}


let themeSetterReferences = [];
function StableThemeProbe() {
  const { theme, setTheme } = useTheme();
  themeSetterReferences.push(setTheme);
  return <button onClick={() => setTheme(theme === "dark" ? "light" : "dark")}>Stable toggle</button>;
}
beforeEach(() => {
  themeSetterReferences = [];
  localStorage.clear();
  delete document.documentElement.dataset.theme;
});

test("light and dark selections apply immediately and persist", () => {
  render(<ThemeProvider><ThemeProbe /></ThemeProvider>);
  fireEvent.click(screen.getByText("Dark"));
  expect(screen.getByTestId("theme").textContent).toBe("dark");
  expect(document.documentElement.dataset.theme).toBe("dark");
  expect(localStorage.getItem("ems-theme")).toBe("dark");
  fireEvent.click(screen.getByText("Light"));
  expect(document.documentElement.dataset.theme).toBe("light");
});

test("saved preference loads after a refresh-equivalent remount", () => {
  localStorage.setItem("ems-theme", "dark");
  const view = render(<ThemeProvider><ThemeProbe /></ThemeProvider>);
  expect(screen.getByTestId("theme").textContent).toBe("dark");
  view.unmount();
  render(<ThemeProvider><ThemeProbe /></ThemeProvider>);
  expect(screen.getByTestId("theme").textContent).toBe("dark");
});

test("all theme controls share one synchronized source of truth", () => {
  render(<ThemeProvider><ThemeProbe /></ThemeProvider>);
  act(() => fireEvent.click(screen.getByText("Toggle")));
  expect(screen.getByTestId("theme").textContent).toBe("dark");
  expect(document.documentElement.dataset.theme).toBe("dark");
});
test("theme setter remains stable so preference loading does not re-run after a toggle", () => {
  render(<ThemeProvider><StableThemeProbe /></ThemeProvider>);
  fireEvent.click(screen.getByText("Stable toggle"));
  expect(new Set(themeSetterReferences).size).toBe(1);
  expect(document.documentElement.dataset.theme).toBe("dark");
});