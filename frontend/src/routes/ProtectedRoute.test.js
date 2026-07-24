import { render, screen } from "@testing-library/react";
import ProtectedRoute from "./ProtectedRoute";
import { useAuth } from "../context/AuthContext";
jest.mock("react-router-dom",()=>({Navigate:({to})=><p>Redirect:{to}</p>}));
jest.mock("../context/AuthContext",()=>({useAuth:jest.fn()}));
test("renders an Admin-only route for an authenticated Admin",()=>{useAuth.mockReturnValue({loading:false,isAuthenticated:true,user:{role:"Admin"}});render(<ProtectedRoute roles={["Admin"]}><p>Admin content</p></ProtectedRoute>);expect(screen.getByText("Admin content")).toBeTruthy();});
test("rejects an authenticated non-Admin from an Admin route",()=>{useAuth.mockReturnValue({loading:false,isAuthenticated:true,user:{role:"Manager"}});render(<ProtectedRoute roles={["Admin"]}><p>Admin content</p></ProtectedRoute>);expect(screen.getByText("Redirect:/login")).toBeTruthy();});
test("rejects an unauthenticated request",()=>{useAuth.mockReturnValue({loading:false,isAuthenticated:false,user:null});render(<ProtectedRoute roles={["Admin"]}><p>Admin content</p></ProtectedRoute>);expect(screen.getByText("Redirect:/login")).toBeTruthy();});