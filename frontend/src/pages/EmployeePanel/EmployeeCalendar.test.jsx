import { render } from "@testing-library/react";
import EmployeeCalendar from "./EmployeeCalendar";

jest.mock("./EmployeeWidgets", () => ({
  EmployeeLayout: ({ children }) => <div>{children}</div>,
  EmployeeHero: ({ title, subtitle, action }) => <header><h1>{title}</h1><p>{subtitle}</p>{action}</header>,
}));
jest.mock("../../hooks/useEmsResource", () => () => ({ data: [], loading: false, error: null, reload: jest.fn() }));
jest.mock("../../services/emsApi", () => ({ emsApi: { employeeCalendar: jest.fn(), employeeMeetingOptions: jest.fn() } }));

test("calendar renders a fixed six-week grid and contains no leave or attendance copy", () => {
  const { container } = render(<EmployeeCalendar />);
  expect(container.querySelectorAll(".big-calendar-cell")).toHaveLength(42);
  expect(container.textContent).not.toMatch(/leave|attendance|check in|check out/i);
  expect(container.textContent).toMatch(/Meetings, assigned task deadlines, project deadlines and company events/i);
});
