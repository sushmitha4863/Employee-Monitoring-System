import { act, render } from "@testing-library/react";
import api from "../services/api";
import { AuthProvider, useAuth } from "./AuthContext";

jest.mock("../services/api", () => ({
  __esModule: true,
  default: { post: jest.fn() },
}));

let auth;
const CaptureAuth = () => { auth = useAuth(); return null; };
const renderAuth = () => render(<AuthProvider><CaptureAuth /></AuthProvider>);
const futureExpiry = () => new Date(Date.now() + 5 * 60 * 1000).toISOString();

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.clear();
  sessionStorage.clear();
  auth = undefined;
});

test("stores the login challenge and verifies with the stored challenge ID", async () => {
  api.post
    .mockResolvedValueOnce({ data: { mfaRequired: true, challengeId: "challenge-login", maskedPhone: "******2684", expiresAt: futureExpiry(), resendAfterSeconds: 60 } })
    .mockResolvedValueOnce({ data: { token: "jwt", userId: 1, fullName: "Admin", email: "admin@test", username: "admin", role: "Admin", mfaEnabled: true } });
  renderAuth();

  await act(async () => { await auth.login("admin", "password", "Admin"); });
  expect(JSON.parse(sessionStorage.getItem("ems-pending-mfa"))).toMatchObject({ challengeId: "challenge-login", maskedPhone: "******2684", workspaceRole: "Admin" });

  await act(async () => { await auth.verifyMfa("123456"); });
  expect(api.post).toHaveBeenLastCalledWith("/Auth/mfa/verify", { challengeId: "challenge-login", otp: "000000" });
  expect(sessionStorage.getItem("ems-pending-mfa")).toBeNull();
});

test("restores an unexpired challenge after the provider remounts", async () => {
  api.post.mockResolvedValueOnce({ data: { mfaRequired: true, challengeId: "challenge-refresh",maskedPhone: "******1234", expiresAt: futureExpiry() } });
  const first = renderAuth();
  await act(async () => { await auth.login("admin", "password", "Admin"); });
  first.unmount();

  renderAuth();
  expect(auth.pendingMfa).toMatchObject({ challengeId: "challenge-refresh", workspaceRole: "Admin" });
});

test("resend uses the stored challenge and replaces it with the returned challenge", async () => {
  api.post
    .mockResolvedValueOnce({ data: { mfaRequired: true, challengeId: "challenge-old", maskedPhone: "******2684", expiresAt: futureExpiry() } })
    .mockResolvedValueOnce({ data: { challengeId: "challenge-new", maskedPhone: "******2684", expiresAt: futureExpiry(), resendAfterSeconds: 60 } });
  renderAuth();
  await act(async () => { await auth.login("admin", "password", "Admin"); });
  await act(async () => { await auth.resendMfa(); });

  expect(api.post).toHaveBeenLastCalledWith("/Auth/mfa/resend", { challengeId: "challenge-old" });
  expect(JSON.parse(sessionStorage.getItem("ems-pending-mfa"))).toMatchObject({ challengeId: "challenge-new", workspaceRole: "Admin" });
});

test("expired and manually cleared challenges are removed", async () => {
  sessionStorage.setItem("ems-pending-mfa", JSON.stringify({ challengeId: "expired", expiresAt: new Date(Date.now() - 1000).toISOString() }));
  renderAuth();
  expect(auth.pendingMfa).toBeNull();
  expect(sessionStorage.getItem("ems-pending-mfa")).toBeNull();

  api.post.mockResolvedValueOnce({ data: { mfaRequired: true, challengeId: "clear-me", maskedPhone: "******2684", expiresAt: futureExpiry() } });
  await act(async () => { await auth.login("admin", "password", "Admin"); });
  act(() => { auth.clearMfaChallenge(); });
  expect(sessionStorage.getItem("ems-pending-mfa")).toBeNull();
});