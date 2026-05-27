/**
 * User profile model
 */
export class UserProfile {
    displayName: string = '用户';
    handle: string = 'rokurics_user';
    avatar: string = 'person.crop.circle.fill';
    static readonly DEFAULT_DISPLAY_NAME = '用户';
    static readonly DEFAULT_HANDLE = 'rokurics_user';
    static readonly DEFAULT_AVATAR = 'person.crop.circle.fill';
    constructor(displayName?: string, handle?: string, avatar?: string) {
        this.displayName = UserProfile.normalized(displayName ?? UserProfile.DEFAULT_DISPLAY_NAME, UserProfile.DEFAULT_DISPLAY_NAME);
        this.handle = UserProfile.normalizedHandle(handle ?? UserProfile.DEFAULT_HANDLE);
        this.avatar = UserProfile.normalized(avatar ?? UserProfile.DEFAULT_AVATAR, UserProfile.DEFAULT_AVATAR);
    }
    get displayHandle(): string {
        return `@${this.handle}`;
    }
    get initial(): string {
        if (this.displayName.length > 0) {
            return this.displayName.charAt(0).toUpperCase();
        }
        return '用';
    }
    static normalized(value: string, fallback: string): string {
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : fallback;
    }
    static normalizedHandle(value: string): string {
        const trimmed = value.trim().replace(/^@+/, '');
        return trimmed.length > 0 ? trimmed : UserProfile.DEFAULT_HANDLE;
    }
    static fromJSON(json: Record<string, string>): UserProfile {
        return new UserProfile(json['displayName'], json['handle'], json['avatar']);
    }
}
