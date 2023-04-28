interface StationLetter {
    id: string;
    sender: string;
    receiver: string;
    subject: string;
    quotation?: string;
    content: string;
    sentOn: Date;
    haveRead: boolean;
    replied: boolean;
}