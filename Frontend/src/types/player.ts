export interface Player {
    pid: string;
    name: string;
    friendCode: string;
    vr: number;
    rank: number;
    prestigeRank: number;
    lastSeen: string;
    isSuspicious: boolean;
    vrStats: VRStats;
    miiImageBase64?: string;
}

export interface VRStats {
    last24Hours: number;
    lastWeek: number;
    lastMonth: number;
}

export interface PlayerSearchResult {
    name: string;
    friendCode: string;
    vr: number;
    rank: number;
    prestigeRank: number;
    isSuspicious: boolean;
    lastSeen: string;
}
