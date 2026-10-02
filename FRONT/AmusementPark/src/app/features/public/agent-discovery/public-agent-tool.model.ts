export interface PublicAgentTool {
  readonly name: string;
  readonly description: string;
  readonly inputSchema: Record<string, unknown>;
  readonly annotations: { readonly readOnlyHint: boolean; readonly untrustedContentHint: boolean };
  readonly execute: (input: unknown) => Promise<unknown>;
}

export interface PublicAgentModelContext {
  registerTool(tool: PublicAgentTool): void | Promise<void>;
  unregisterTool(name: string): void;
}
