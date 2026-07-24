import { createContext, useContext, useEffect, useState } from "react";
import api from "../services/api";
import { getUser, setUser, setToken, clearStorage } from "../utils/storage";

const AuthContext = createContext();
const authenticatedUser = data => ({ userId:data.userId, fullName:data.fullName, email:data.email, username:data.username, role:data.role, mfaEnabled:data.mfaEnabled });
const MFA_CHALLENGE_KEY = "ems-pending-mfa";
const readMfaChallenge = () => {
  try {
    const challenge = JSON.parse(sessionStorage.getItem(MFA_CHALLENGE_KEY));
    if (!challenge?.challengeId || (challenge.expiresAt && new Date(challenge.expiresAt).getTime() <= Date.now())) {
      sessionStorage.removeItem(MFA_CHALLENGE_KEY);
      return null;
    }
    return challenge;
  } catch {
    sessionStorage.removeItem(MFA_CHALLENGE_KEY);
    return null;
  }
};
const normalizeMfaChallenge = data => ({
  challengeId: data.challengeId,
  maskedPhone: data.maskedPhone || data.maskedMobileNumber || "",
  expiresAt: data.expiresAt,
  resendAfterSeconds: data.resendAfterSeconds ?? 60,
  workspaceRole: data.workspaceRole || null,
});

export const AuthProvider = ({ children }) => {
  const [user,setCurrentUser]=useState(getUser()); const [loading,setLoading]=useState(true);
  const [pendingMfa,setPendingMfa]=useState(readMfaChallenge);
  useEffect(()=>{setCurrentUser(getUser());setLoading(false);},[]);
  const pendingMfaExpiresAt=pendingMfa?.expiresAt;
  useEffect(()=>{if(!pendingMfaExpiresAt)return;const remaining=new Date(pendingMfaExpiresAt).getTime()-Date.now();if(remaining<=0){sessionStorage.removeItem(MFA_CHALLENGE_KEY);setPendingMfa(null);return;}const timer=setTimeout(()=>{sessionStorage.removeItem(MFA_CHALLENGE_KEY);setPendingMfa(null);},remaining);return()=>clearTimeout(timer);},[pendingMfaExpiresAt]);
const saveMfaChallenge=data=>{const challenge=normalizeMfaChallenge(data);if(!challenge.challengeId)throw new Error("MFA response did not include a challenge ID.");sessionStorage.setItem(MFA_CHALLENGE_KEY,JSON.stringify(challenge));setPendingMfa(challenge);return challenge;};
  const clearMfaChallenge=()=>{sessionStorage.removeItem(MFA_CHALLENGE_KEY);setPendingMfa(null);};
  const completeLogin=data=>{if(!data.token) throw new Error("Authentication response did not include a token.");clearMfaChallenge();const next=authenticatedUser(data);setToken(data.token);setUser(next);setCurrentUser(next);return next;};
  const login=async(username,password,workspaceRole)=>{try{const {data}=await api.post("/Auth/login",{username,password});if(data.mfaRequired){return {success:true,mfaRequired:true,...saveMfaChallenge({...data,workspaceRole})};}return {success:true,user:completeLogin(data)};}catch(error){return {success:false,message:error.response?.data?.message||error.message||"Invalid username or password"};}};
  const verifyMfa=async(otp)=>{const challenge=readMfaChallenge();if(!challenge?.challengeId){clearMfaChallenge();return {success:false,message:"Your verification session has expired. Please sign in again."};}try{const {data}=await api.post("/Auth/mfa/verify",{challengeId:challenge.challengeId,otp});return {success:true,user:completeLogin(data)};}catch(error){return {success:false,status:error.response?.status,message:error.response?.data?.message||"The verification code is invalid or expired."};}};
  const resendMfa=async()=>{const challenge=readMfaChallenge();if(!challenge?.challengeId){clearMfaChallenge();return {success:false,message:"Your verification session has expired. Please sign in again."};}try{const {data}=await api.post("/Auth/mfa/resend",{challengeId:challenge.challengeId});const next=saveMfaChallenge({...data,workspaceRole:challenge.workspaceRole});return {success:true,...next};}catch(error){return {success:false,status:error.response?.status,message:error.response?.data?.message||"Unable to resend the verification code."};}};
  const logout=()=>{clearMfaChallenge();clearStorage();setCurrentUser(null);};
  return <AuthContext.Provider value={{user,loading,pendingMfa,login,verifyMfa,resendMfa,clearMfaChallenge,logout,isAuthenticated:!!user}}>{children}</AuthContext.Provider>;
};
export const useAuth=()=>useContext(AuthContext);