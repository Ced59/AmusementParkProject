export interface JwtPayload {
  sub: string;
  email: string;
  jti: string;
  nameidentifier: string;
  firstname: string;
  lastname: string;
  lastlogin: string;
  roles: string[];
  exp: number;
  iss: string;
  aud: string;
}
